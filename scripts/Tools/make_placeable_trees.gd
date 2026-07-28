@tool
extends EditorScript

# Erzeugt aus den reinen Baum-Meshes in merged/ platzierbare Baum-Szenen MIT Kollision.
# Die neuen Szenen zieht man einfach per Drag&Drop in die Welt - Kollision ist schon drin,
# tree_collision.gd wird fuer diese Baeume nicht gebraucht.
#
# Aufbau der erzeugten Szene:
#   Eiche_25            (StaticBody3D)   <- Wurzel, das ist der Kollisionskoerper
#     +- Mesh           (MeshInstance3D) <- das Baum-Mesh aus merged/
#     +- CollisionShape3D               <- Zylinder um den STAMM (nicht um die Krone)
#
# BENUTZUNG:
#   1. Diese Datei im Godot-Script-Editor oeffnen.
#   2. Menue "Datei" -> "Ausfuehren" (Strg+Umschalt+X).
#   3. Erst mit DRY_RUN = true laufen lassen, Zahlen in der Ausgabe pruefen,
#      dann DRY_RUN = false und erneut ausfuehren.
#
# merged/ bleibt unangetastet - das ist weiterhin die Quelle fuer den Terrain3D-Instancer.

const SRC_DIR := "res://Assets/Nature/Forest/merged/"
const OUT_DIR := "res://Assets/Nature/Forest/placeable/"

## true = nur rechnen und ausgeben, nichts schreiben.
const DRY_RUN := false

## "" = alle. Zum Testen z.B. "oak_25.tscn".
const ONLY_FILE := ""

## Vorhandene Dateien in OUT_DIR ueberschreiben.
const OVERWRITE := true

## Pflanzen niedriger als das bekommen KEINE Kollision (Straeucher, Saplings).
## Die Szene wird trotzdem erzeugt, nur ohne Kollisionskoerper.
const MIN_TREE_HEIGHT := 4.0

## Hoeher als das muss der Kollisionszylinder nicht sein - da kommt kein Spieler hin.
const MAX_COLLIDER_HEIGHT := 6.0

## Aus welchem Teil des Baumes (von unten) der Stammradius gemessen wird.
## 0.15 = unterste 15 % der Hoehe. Dort sind fast nur Stamm-Vertices, keine Aeste.
const TRUNK_SLICE := 0.15

## Statt des groessten Abstands den Wert bei diesem Anteil nehmen - wirft Ausreisser
## (Wurzelanlaeufe, einzelne tiefe Aeste) raus. 0.9 = 90. Perzentil.
const RADIUS_PERCENTILE := 0.9

## Sicherheitszuschlag auf den gemessenen Stammradius (Spieler soll nicht im Stamm kleben).
const RADIUS_PADDING := 0

## Untergrenze, falls die Messung Unsinn liefert.
const MIN_RADIUS := 0.15

## Materialnamen, die einen Stamm kennzeichnen. Wird nur zum Eingrenzen benutzt;
## findet sich keiner, werden alle Surfaces gemessen (die Hoehen-Scheibe reicht meist schon).
const TRUNK_HINTS := ["bark", "trunk", "stem", "wood", "log"]


func _run() -> void:
	var da := DirAccess.open(SRC_DIR)
	if da == null:
		push_error("Quellordner nicht gefunden: " + SRC_DIR)
		return

	if not DRY_RUN:
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))

	var erzeugt := 0
	var ohne_kollision := 0
	var uebersprungen := 0

	print("--- Platzierbare Baeume aus %s ---" % SRC_DIR)

	for f in da.get_files():
		if not f.ends_with(".tscn"):
			continue
		if ONLY_FILE != "" and f != ONLY_FILE:
			continue

		var out_path := OUT_DIR + f
		if not OVERWRITE and ResourceLoader.exists(out_path):
			print("  %s: existiert schon, uebersprungen" % f)
			uebersprungen += 1
			continue

		var ergebnis := _build(f, out_path)
		match ergebnis:
			1:
				erzeugt += 1
			2:
				erzeugt += 1
				ohne_kollision += 1
			_:
				uebersprungen += 1

	print("---")
	if DRY_RUN:
		print("DRY_RUN = true -> nichts geschrieben. %d Szenen waeren erzeugt worden (davon %d ohne Kollision), %d uebersprungen."
			% [erzeugt, ohne_kollision, uebersprungen])
		print("Zum Schreiben DRY_RUN auf false setzen und erneut ausfuehren.")
	else:
		print("Fertig: %d Szenen in %s (davon %d ohne Kollision), %d uebersprungen."
			% [erzeugt, OUT_DIR, ohne_kollision, uebersprungen])
		EditorInterface.get_resource_filesystem().scan()


## Rueckgabe: 0 = Fehler/uebersprungen, 1 = mit Kollision, 2 = ohne Kollision
func _build(file_name: String, out_path: String) -> int:
	var ps := load(SRC_DIR + file_name) as PackedScene
	if ps == null:
		push_warning("%s: laesst sich nicht laden" % file_name)
		return 0

	var quelle: Node = ps.instantiate()
	var mi := _find_mesh(quelle)
	if mi == null or mi.mesh == null:
		push_warning("%s: kein Mesh gefunden" % file_name)
		quelle.free()
		return 0

	var mesh: Mesh = mi.mesh
	var cast_shadow: int = mi.cast_shadow
	var mesh_name := String(quelle.name)

	var mass := _messe_stamm(mesh)
	quelle.free()

	if mass.is_empty():
		push_warning("%s: keine Vertices lesbar" % file_name)
		return 0

	var hoehe: float = mass["hoehe"]
	var basis_y: float = mass["basis_y"]
	var radius: float = mass["radius"]
	var mitte: Vector2 = mass["mitte"]

	var mit_kollision := hoehe >= MIN_TREE_HEIGHT
	var zyl_hoehe: float = minf(hoehe, MAX_COLLIDER_HEIGHT)

	if mit_kollision:
		print("  %-32s H=%5.1fm  Stammradius=%4.2fm  Zylinder H=%4.1fm  Mitte=(%.2f, %.2f)"
			% [file_name, hoehe, radius, zyl_hoehe, mitte.x, mitte.y])
	else:
		print("  %-32s H=%5.1fm  -> unter %.1fm, keine Kollision"
			% [file_name, hoehe, MIN_TREE_HEIGHT])

	if DRY_RUN:
		return 1 if mit_kollision else 2

	# --- Szene bauen ---
	var wurzel: Node3D
	if mit_kollision:
		wurzel = StaticBody3D.new()
	else:
		# Ohne Kollision braucht es keinen Physik-Koerper als Wurzel.
		wurzel = Node3D.new()
	wurzel.name = mesh_name

	var neu_mi := MeshInstance3D.new()
	neu_mi.name = "Mesh"
	neu_mi.mesh = mesh
	neu_mi.cast_shadow = cast_shadow
	wurzel.add_child(neu_mi)
	neu_mi.owner = wurzel

	if mit_kollision:
		var form := CylinderShape3D.new()
		form.radius = radius
		form.height = zyl_hoehe
		var cs := CollisionShape3D.new()
		cs.name = "CollisionShape3D"
		cs.shape = form
		# CylinderShape3D sitzt mittig um seinen Ursprung -> auf halbe Hoehe ueber die Basis.
		cs.position = Vector3(mitte.x, basis_y + zyl_hoehe * 0.5, mitte.y)
		wurzel.add_child(cs)
		cs.owner = wurzel

	var packed := PackedScene.new()
	var err := packed.pack(wurzel)
	if err == OK:
		err = ResourceSaver.save(packed, out_path)
	wurzel.free()

	if err != OK:
		push_error("%s: Speichern fehlgeschlagen (Fehler %d)" % [file_name, err])
		return 0

	return 1 if mit_kollision else 2


## Misst Gesamthoehe und den Radius des Stammes aus den Mesh-Vertices.
func _messe_stamm(mesh: Mesh) -> Dictionary:
	var alle: PackedVector3Array = PackedVector3Array()
	var stamm: PackedVector3Array = PackedVector3Array()

	for s in mesh.get_surface_count():
		var arrays: Array = mesh.surface_get_arrays(s)
		if arrays.is_empty() or arrays[Mesh.ARRAY_VERTEX] == null:
			continue
		var verts: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
		alle.append_array(verts)
		if _ist_stamm_surface(mesh, s):
			stamm.append_array(verts)

	if alle.is_empty():
		return {}

	# Hoehe immer aus dem GANZEN Baum (Krone gehoert dazu).
	var min_y: float = alle[0].y
	var max_y: float = alle[0].y
	for v in alle:
		min_y = minf(min_y, v.y)
		max_y = maxf(max_y, v.y)
	var hoehe := max_y - min_y

	# Radius nur aus dem Stamm-Material, sonst aus allem.
	var quelle: PackedVector3Array = stamm if not stamm.is_empty() else alle

	# Nur die unterste Scheibe - dort ist der Stamm, keine Aeste.
	var grenze := min_y + hoehe * TRUNK_SLICE
	var scheibe: Array[Vector2] = []
	for v in quelle:
		if v.y <= grenze:
			scheibe.append(Vector2(v.x, v.z))
	if scheibe.is_empty():
		return {}

	# Achse des Stammes = Mittel der Scheibe (die Modelle stehen meist auf 0/0, aber nicht alle).
	var mitte := Vector2.ZERO
	for p in scheibe:
		mitte += p
	mitte /= float(scheibe.size())

	var abstaende: Array[float] = []
	for p in scheibe:
		abstaende.append(p.distance_to(mitte))
	abstaende.sort()

	var idx := int(float(abstaende.size() - 1) * RADIUS_PERCENTILE)
	idx = clampi(idx, 0, abstaende.size() - 1)
	var radius: float = maxf(abstaende[idx] + RADIUS_PADDING, MIN_RADIUS)

	return {
		"hoehe": hoehe,
		"basis_y": min_y,
		"radius": radius,
		"mitte": mitte,
	}


func _ist_stamm_surface(mesh: Mesh, surface: int) -> bool:
	var mat: Material = mesh.surface_get_material(surface)
	if mat == null:
		return false
	var n := mat.resource_name.to_lower()
	if n == "":
		return false
	for hinweis in TRUNK_HINTS:
		if n.contains(hinweis):
			return true
	return false


func _find_mesh(node: Node) -> MeshInstance3D:
	if node is MeshInstance3D:
		return node as MeshInstance3D
	for c in node.get_children():
		var treffer := _find_mesh(c)
		if treffer != null:
			return treffer
	return null
