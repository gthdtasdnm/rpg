@tool
extends EditorScript

# Erzeugt fuer jeden Baum einen "Imposter": ein Bild des Baums, das in der Ferne anstelle des
# echten Modells angezeigt wird und sich immer zur Kamera dreht.
#
# WARUM NICHT EINFACH VEREINFACHEN:
# Ein vereinfachtes Blattwerk hat WENIGER Blaetter - der Wald wird in der Ferne noch duenner,
# genau das Gegenteil vom Ziel. Ein Imposter ist dagegen eine geschlossene Flaeche: aus der
# Entfernung volle Baumkronen statt einzelner Aeste, und er kostet zwei Dreiecke.
#
# BENUTZUNG:
#   1. Datei im Godot-Script-Editor oeffnen.
#   2. Menue "Datei" -> "Ausfuehren" (Strg+Umschalt+X).
#   3. Erst mit ONLY_FILE einen einzelnen Baum testen, Bild anschauen, dann alle.
#   4. Danach in World/data/assets.tres "last_lod = 3" setzen (mache ich auf Zuruf).
#
# HINWEIS: Hier wird bewusst KEIN "await" benutzt. Ein EditorScript bricht beim ersten await
# ab und tut dann gar nichts. Stattdessen zeichnet RenderingServer.force_draw() sofort.

const DIR := "res://Assets/Nature/Forest/merged/"
const IMPOSTER_DIR := "res://Assets/Nature/Forest/imposters/"

## Zum Ausprobieren erst EINEN Baum: z.B. "oak_25.tscn". Leer = alle.
const ONLY_FILE := ""

## Kantenlaenge EINER Ansicht im Atlas. Bei 4 Ansichten ist das Bild doppelt so gross (2x2).
const TEXTURE_SIZE := 512

## Wie viele Ansichten pro Baum, gleichmaessig um die Hochachse verteilt. Sie landen als
## Raster in einem Bild (4 -> 2x2). Ein Shader waehlt pro Baum-Instanz eine davon aus, damit
## nebeneinander stehende Baeume derselben Art nicht identisch aussehen.
## Muss eine Quadratzahl sein: 1, 4, 9 oder 16.
const VARIANTS := 4

const SHADER_PATH := IMPOSTER_DIR + "imposter.gdshader"
const SHADOW_SHADER_PATH := IMPOSTER_DIR + "imposter_shadow.gdshader"

## Groesse des angedeuteten Bodenschattens, im Verhaeltnis zur Kronenbreite.
const SHADOW_SIZE := 1.15

## Wie dunkel der Bodenschatten in der Mitte ist (0 = aus).
const SHADOW_STRENGTH := 0.0  # 0 = kein Blob-Schatten (kollidierte mit der Überblendung)

## true = vorhandene Imposter werden neu erzeugt. Beim Einstellen der Bildlage noetig, sonst
## ueberspringt das Script den Baum und man sieht weiterhin das alte Bild.
const OVERWRITE := true


func _run() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(IMPOSTER_DIR))

	var da := DirAccess.open(DIR)
	if da == null:
		push_error("Ordner nicht gefunden: " + DIR)
		return

	var done := 0
	for f in da.get_files():
		if not f.ends_with(".tscn"):
			continue
		if ONLY_FILE != "" and f != ONLY_FILE:
			continue
		if _build(f):
			done += 1

	EditorInterface.get_resource_filesystem().scan()
	print("--- fertig: %d Imposter ---" % done)
	if ONLY_FILE != "" and done > 0:
		print("Das war nur '%s'. Bild ansehen, dann ONLY_FILE leeren und erneut ausführen." % ONLY_FILE)


func _build(file_name: String) -> bool:
	var path := DIR + file_name
	var ps := load(path) as PackedScene
	if ps == null:
		push_warning("%s: konnte nicht geladen werden" % file_name)
		return false

	var tree_root: Node = ps.instantiate()

	if not OVERWRITE:
		for c in tree_root.get_children():
			if c.name == "LOD1":
				tree_root.free()
				print("%s: hat schon einen Imposter — übersprungen (OVERWRITE = false)" % file_name)
				return false

	# Beim Neuerzeugen immer vom echten Baum ausgehen, nicht von einer vorherigen
	# Imposter-Ebene: LOD0 ist das Original, sonst der einzige Mesh-Knoten.
	var source := tree_root.get_node_or_null("LOD0") as MeshInstance3D
	if source == null:
		source = _find_mesh(tree_root)
	if source == null or source.mesh == null:
		push_warning("%s: kein Mesh gefunden" % file_name)
		tree_root.free()
		return false

	var mesh: Mesh = source.mesh
	var casts := source.cast_shadow
	var aabb: AABB = _real_aabb(mesh)

	# Bewusst grosszuegig rendern und danach auf den Baum zuschneiden (siehe _crop_to_content).
	# Das ist unabhaengig davon, ob die Bounding Box des Meshes stimmt - und bei den
	# zusammengefuegten Baeumen stimmt sie nachweislich nicht.
	var view_size := _view_size(aabb)
	var grid := int(sqrt(float(VARIANTS)))

	# Alle Ansichten rendern, dann den GEMEINSAMEN Ausschnitt bestimmen: jede Ansicht muss
	# denselben Bildbereich zeigen, sonst wackelt der Baum beim Wechsel der Variante.
	var views: Array[Image] = []
	var crop := Rect2i(0, 0, 0, 0)

	for i in VARIANTS:
		var angle := TAU * float(i) / float(VARIANTS)
		var view := _render(mesh, aabb, view_size, angle)
		if view == null or view.is_empty():
			push_warning("%s: Rendern fehlgeschlagen" % file_name)
			tree_root.free()
			return false

		views.append(view)
		var c := _crop_to_content(view)
		crop = c if crop.size.x <= 0 else crop.merge(c)

	if crop.size.x <= 0:
		push_warning("%s: Bild ist leer - nichts gerendert" % file_name)
		tree_root.free()
		return false

	# Quadratisch machen, damit alle Zellen gleich gross sind und die Ebene nicht verzerrt.
	# Wichtig: seitlich mittig erweitern, nach oben auffuellen - die UNTERKANTE bleibt genau
	# am Baumfuss. Dadurch ist die Ebene unten buendig mit dem Boden, und die Hoehe ergibt
	# sich schlicht aus ihrer halben Groesse (siehe center_offset weiter unten). Vorher wurde
	# oben und unten gleich viel Rand angesetzt - dadurch schwebten die Baeume.
	var side: int = maxi(crop.size.x, crop.size.y)
	var bottom: int = crop.position.y + crop.size.y
	crop.position.x -= (side - crop.size.x) / 2
	crop.position.y = bottom - side
	crop.size = Vector2i(side, side)
	crop = crop.intersection(Rect2i(0, 0, TEXTURE_SIZE, TEXTURE_SIZE))

	var image := _build_atlas(views, crop, grid)

	# Aus dem Ausschnitt zurueckrechnen, wie gross der Baum in Metern ist und wo seine Mitte
	# liegt. Pixel -> Meter: der gerenderte Bereich war view_size Meter breit.
	var per_pixel := view_size / float(TEXTURE_SIZE)
	var quad_w: float = crop.size.x * per_pixel
	var quad_h: float = crop.size.y * per_pixel
	# Die Unterkante des Ausschnitts liegt am Baumfuss (siehe oben), also sitzt die Ebene
	# genau eine halbe Hoehe ueber dem Boden. Kein Rueckrechnen aus Kameraposition oder
	# Bounding Box noetig - beides war fehleranfaellig.
	var center_y: float = quad_h * 0.5

	var tex_path := IMPOSTER_DIR + file_name.replace(".tscn", ".png")
	var save_err := image.save_png(ProjectSettings.globalize_path(tex_path))
	if save_err != OK:
		push_error("%s: PNG konnte nicht geschrieben werden (%d)" % [file_name, save_err])

	# Textur direkt aus dem Bild bauen statt die PNG-Datei zu laden: die ist gerade erst
	# entstanden und Godot kennt sie noch nicht. Sie wird in der Szene mitgespeichert.
	var texture := ImageTexture.create_from_image(image)

	_assemble(tree_root, mesh, casts, texture, Vector2(quad_w, quad_h), center_y, grid, path, file_name)
	return true


## Setzt die Ansichten als Raster in ein Bild. Jede Zelle zeigt denselben Ausschnitt, damit der
## Baum beim Wechsel der Variante nicht springt.
func _build_atlas(views: Array[Image], crop: Rect2i, grid: int) -> Image:
	var cell := crop.size.x
	var atlas := Image.create(cell * grid, cell * grid, false, Image.FORMAT_RGBA8)
	atlas.fill(Color(0, 0, 0, 0))

	for i in views.size():
		var piece := views[i].get_region(crop)
		var x := (i % grid) * cell
		var y := int(i / grid) * cell
		atlas.blit_rect(piece, Rect2i(Vector2i.ZERO, piece.get_size()), Vector2i(x, y))

	return atlas


## Zeichnet das Mesh von vorne in eine unsichtbare Ansicht: transparenter Hintergrund,
## flaches Licht (der Imposter soll die Grundfarbe zeigen, keine harten Schatten).
func _render(mesh: Mesh, aabb: AABB, view_size: float, angle: float) -> Image:
	var vp := SubViewport.new()
	vp.size = Vector2i(TEXTURE_SIZE, TEXTURE_SIZE)
	vp.transparent_bg = true
	vp.own_world_3d = true
	vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	var world := World3D.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_CLEAR_COLOR
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.62, 0.70, 0.78) # leicht himmelblau von oben
	# Wenig Umgebungslicht, dafuer eine klare Lichtrichtung: sonst wird das Bild flach und
	# einfarbig, und der Imposter faellt neben den echten, schattierten Baeumen sofort auf.
	env.ambient_light_energy = 0.45
	world.environment = env
	vp.world_3d = world

	# Baum bleibt, wo er ist - stattdessen wird die Kamera auf seine Mitte ausgerichtet.
	# (Das Mesh zu verschieben ging schief: der Baumfuß landete in der Bildmitte und die
	# Krone wurde oben abgeschnitten.)
	# Fuer die Varianten wird der Baum gedreht, nicht die Kamera - so bleibt die Beleuchtung
	# aus derselben Richtung und die Ansichten passen zueinander.
	var mi := MeshInstance3D.new()
	mi.mesh = mesh
	if angle != 0.0:
		var center := aabb.get_center()
		mi.transform = Transform3D(Basis(Vector3.UP, angle), Vector3.ZERO) \
			.translated_local(-center).translated(center)
	vp.add_child(mi)

	# Hauptlicht von schraeg oben - erzeugt die Hell/Dunkel-Verteilung in der Krone.
	var light := DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-40, -35, 0)
	light.light_energy = 1.35
	vp.add_child(light)

	# Schwaches Gegenlicht, damit die Schattenseite nicht schwarz absaeuft.
	var fill := DirectionalLight3D.new()
	fill.rotation_degrees = Vector3(-15, 150, 0)
	fill.light_energy = 0.35
	fill.light_color = Color(0.75, 0.82, 0.9)
	vp.add_child(fill)

	var center := aabb.get_center()
	var cam := Camera3D.new()
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = view_size
	cam.position = Vector3(center.x, center.y, center.z + view_size * 4.0)
	cam.near = 0.01
	cam.far = view_size * 12.0
	vp.add_child(cam)

	EditorInterface.get_base_control().add_child(vp)

	# Mehrfach zeichnen: der erste Durchgang laedt Texturen und baut Shader, erst danach
	# steht das fertige Bild.
	for i in 3:
		RenderingServer.force_draw()

	var image := vp.get_texture().get_image()
	EditorInterface.get_base_control().remove_child(vp)
	vp.queue_free()
	return image


## Baut die Szene neu: LOD0 = Originalmesh, LOD3 = Imposter-Ebene.
func _assemble(old_root: Node, mesh: Mesh, casts: int, texture: Texture2D,
		quad_size: Vector2, center_y: float, grid: int, scene_path: String, file_name: String) -> void:
	var new_root := Node3D.new()
	new_root.name = old_root.name

	var lod0 := MeshInstance3D.new()
	lod0.name = "LOD0"
	lod0.mesh = mesh
	lod0.cast_shadow = casts
	new_root.add_child(lod0)
	lod0.owner = new_root

	# Groesse und Hoehe kommen aus dem zugeschnittenen Bild - dadurch passt der Imposter
	# exakt auf den echten Baum, unabhaengig von der Bounding Box des Meshes.
	var quad := QuadMesh.new()
	quad.size = quad_size
	# Die Hoehe MUSS ins Mesh selbst: Terrain3D uebernimmt aus der Szene nur das Mesh und
	# setzt es an die Instanzposition - die Position des Knotens wird verworfen. Ohne
	# center_offset sitzt die Ebene mittig auf dem Boden und der Baum steckt halb darin.
	quad.center_offset = Vector3(0, center_y, 0)

	# Eigener Shader statt StandardMaterial3D: er macht das Billboard UND waehlt pro Instanz
	# eine der Ansichten aus dem Atlas (siehe imposter.gdshader).
	var shader := load(SHADER_PATH) as Shader
	if shader == null:
		push_error("Shader nicht gefunden: " + SHADER_PATH)
		old_root.free()
		new_root.free()
		return

	var mat := ShaderMaterial.new()
	mat.shader = shader
	mat.set_shader_parameter("albedo_texture", texture)
	mat.set_shader_parameter("grid", grid)
	mat.set_shader_parameter("alpha_cut", 0.35)

	# Waagerechte Flaeche am Boden als angedeuteter Schatten (siehe imposter_shadow.gdshader).
	# Ohne sie haetten alle Baeume jenseits der LOD-Grenze keinen Schatten, waehrend das
	# Terrain noch beschattet wird - die sichtbare Kante im Wald.
	var ground := PlaneMesh.new()
	ground.size = Vector2(quad_size.x, quad_size.x) * SHADOW_SIZE
	# Knapp ueber den Boden, sonst kaempfen Boden und Schatten um dieselbe Tiefe und es flackert.
	ground.center_offset = Vector3(0, 0.35, 0)

	var shadow_mat := ShaderMaterial.new()
	var shadow_shader := load(SHADOW_SHADER_PATH) as Shader
	if shadow_shader != null:
		shadow_mat.shader = shadow_shader
		shadow_mat.set_shader_parameter("strength", SHADOW_STRENGTH)
	else:
		push_warning("Schatten-Shader nicht gefunden: " + SHADOW_SHADER_PATH)

	# Beides in EIN Mesh mit zwei Flaechen: Terrain3D uebernimmt pro LOD-Stufe nur ein Mesh.
	# Jede Flaeche behaelt ihr eigenes Material, das Billboard betrifft also nur den Baum
	# und nicht den Bodenschatten.
	var combined := ArrayMesh.new()
	combined.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, quad.get_mesh_arrays())
	combined.surface_set_material(0, mat)
	if shadow_shader != null:
		combined.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, ground.get_mesh_arrays())
		combined.surface_set_material(1, shadow_mat)

	var lod3 := MeshInstance3D.new()
	lod3.name = "LOD1"
	lod3.mesh = combined
	# Knoten bleibt im Ursprung - die Hoehe steckt ausschliesslich im Mesh (center_offset oben).
	# Beides zusammen hiesse doppelte Verschiebung, und die Baeume schwebten.
	lod3.position = Vector3.ZERO
	lod3.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	new_root.add_child(lod3)
	lod3.owner = new_root

	var packed := PackedScene.new()
	packed.pack(new_root)
	var err := ResourceSaver.save(packed, scene_path)
	old_root.free()
	new_root.free()

	if err != OK:
		push_error("Konnte %s nicht speichern (Fehler %d)" % [file_name, err])
		return

	print("%s: %d Ansichten, %.1f x %.1f m, Mitte auf y=%.1f"
		% [file_name, grid * grid, quad_size.x, quad_size.y, center_y])


## Ermittelt die tatsaechliche Ausdehnung aus den Eckpunkten.
##
## mesh.get_aabb() ist hier NICHT verlaesslich: die merged-Meshes wurden per Script
## zusammengesetzt (merge_tree_meshes.gd), dabei ist die gespeicherte Bounding Box nicht
## mitgewachsen. Sie meldet eine Mitte von (0,0,0), obwohl der Baum von 0 bis ~15 m reicht -
## die Kamera zielte dadurch auf den Boden und der Baum lag in der oberen Bildhaelfte.
func _real_aabb(mesh: Mesh) -> AABB:
	var result := AABB()
	var started := false

	for s in mesh.get_surface_count():
		var arrays: Array = mesh.surface_get_arrays(s)
		if arrays.is_empty():
			continue
		var verts: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
		for v in verts:
			if not started:
				result = AABB(v, Vector3.ZERO)
				started = true
			else:
				result = result.expand(v)

	if not started:
		return mesh.get_aabb()

	return result


## Kantenlaenge des quadratischen Bildausschnitts: die groessere der beiden Ausdehnungen,
## plus etwas Rand. Wird beim Rendern UND fuer die Ebene benutzt, damit beide zusammenpassen.
func _view_size(aabb: AABB) -> float:
	# Faktor 2 statt knapp 1: lieber zu viel Rand als ein abgeschnittener Baum. Der Rand wird
	# gleich wieder weggeschnitten, es geht also nur Aufloesung verloren, nie Bildinhalt.
	var largest := maxf(maxf(aabb.size.x, aabb.size.z), aabb.size.y)
	return maxf(largest, 1.0) * 2.0


## Findet den Bereich des Bildes, in dem tatsaechlich etwas zu sehen ist (alles, was nicht
## voellig durchsichtig ist). Damit richtet sich der Imposter am gerenderten Ergebnis aus
## statt an Zahlen aus dem Mesh - fehlerhafte Bounding Boxes spielen dann keine Rolle mehr.
func _crop_to_content(image: Image) -> Rect2i:
	var min_x := image.get_width()
	var min_y := image.get_height()
	var max_x := -1
	var max_y := -1

	for y in image.get_height():
		for x in image.get_width():
			if image.get_pixel(x, y).a <= 0.02:
				continue
			min_x = mini(min_x, x)
			min_y = mini(min_y, y)
			max_x = maxi(max_x, x)
			max_y = maxi(max_y, y)

	if max_x < 0:
		return Rect2i(0, 0, 0, 0)

	# Ein Pixel Rand, damit die Blattkanten beim Skalieren nicht ausfransen.
	min_x = maxi(0, min_x - 1)
	min_y = maxi(0, min_y - 1)
	max_x = mini(image.get_width() - 1, max_x + 1)
	max_y = mini(image.get_height() - 1, max_y + 1)

	return Rect2i(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1)


func _find_mesh(node: Node) -> MeshInstance3D:
	if node is MeshInstance3D:
		return node
	for c in node.get_children():
		var found := _find_mesh(c)
		if found != null:
			return found
	return null
