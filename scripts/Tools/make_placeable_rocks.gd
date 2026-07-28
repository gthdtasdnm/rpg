@tool
extends EditorScript

# Macht aus den .glb-Felsen in Assets/Nature/Rocks/ platzierbare Szenen in Objects/rocks/ -
# gleicher Aufbau wie Objects/trees/, also direkt per Drag&Drop benutzbar.
#
#   rock_09m_outcrop      (StaticBody3D)
#     +- Mesh             (MeshInstance3D)
#     +- CollisionShape3D
#
# Die neuen Namen sind nach GROESSE sortierbar: rock_<maxmass>m_<form>.
# Dadurch ist der Ordner selbst ein Katalog - oben die grossen Wandstuecke,
# unten die Kiesel. Loest das Problem, dass man im Dateibaum nichts erkennt.
#
# BENUTZUNG: Datei -> Ausfuehren (Strg+Umschalt+X). Erst mit DRY_RUN = true.

const SRC_DIR := "res://Assets/Nature/Rocks/"
const OUT_DIR := "res://Objects/rocks/"

## true = nur rechnen und ausgeben, nichts schreiben.
const DRY_RUN := false

## Vorhandene Dateien in OUT_DIR ueberschreiben.
const OVERWRITE := true

## Basis des Felsens auf y = 0 legen. Dann sitzt er beim Platzieren sauber auf dem Boden
## und "einsinken lassen" ist ein bewusstes negatives Y statt Raterei.
const ALIGN_BASE_TO_ZERO := true

## Ab dieser groessten Kantenlaenge (m) Trimesh-Kollision (genau, begehbar).
const TRIMESH_AB := 5.0

## Ab dieser groessten Kantenlaenge (m) Konvex-Kollision (billig, man stoesst dagegen).
## Alles darunter bekommt GAR KEINE Kollision - ueber Kiesel soll man laufen koennen,
## sonst hakt der Spieler staendig.
const CONVEX_AB := 1.2

## Alte Datei -> neuer Name (ohne .tscn).
## Die Groessenangabe ist gemessen, das Formwort ist aus Proportionen und Materialnamen
## abgeleitet - NICHT gesehen. Wenn ein Wort nicht passt: hier die Zeile aendern.
const NAMES := {
	"rock_tor2.glb": "rock_20m_arch",
	"Rock_tor.glb": "rock_17m_arch",
	"rock_formation.glb": "rock_16m_field",
	"mountain_rocks.glb": "rock_09m_outcrop",
	"rock_formation2.glb": "rock_09m_scree",
	"big_long_rock.glb": "rock_09m_ridge",
	"big_flat_beach_rock.glb": "rock_08m_slab_flat",
	"beach_rocks2.glb": "rock_06m_slabs_b",
	"beach_rocks.glb": "rock_06m_slabs_a",
	"mossy_flat_rock.glb": "rock_05m_slab_mossy",
	"rockwall3.glb": "rock_05m_wall",
	"big_flat_rock.glb": "rock_04m_block",
	"mossy_rock2.glb": "rock_02m_slab_mossy",
	"small_rock2.glb": "rock_02m_shard",
	"small_rock.glb": "rock_01m_stone",
	"mossy_rock.glb": "rock_01m_stone_mossy",
	"small_rocks.glb": "rock_01m_pebbles",
}


func _run() -> void:
	var da := DirAccess.open(SRC_DIR)
	if da == null:
		push_error("Quellordner nicht gefunden: " + SRC_DIR)
		return

	if not DRY_RUN:
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))

	var erzeugt := 0
	var uebersprungen := 0
	print("--- Felsen aus %s -> %s ---" % [SRC_DIR, OUT_DIR])

	for f in da.get_files():
		if not f.ends_with(".glb"):
			continue

		var neuer_name: String = NAMES.get(f, "")
		if neuer_name == "":
			# Unbekannte Datei: Originalnamen behalten, damit nichts verloren geht.
			neuer_name = f.get_basename().to_snake_case()
			push_warning("%s: kein Name in NAMES hinterlegt, benutze '%s'" % [f, neuer_name])

		var out_path := OUT_DIR + neuer_name + ".tscn"
		if not OVERWRITE and ResourceLoader.exists(out_path):
			print("  %s: existiert schon, uebersprungen" % neuer_name)
			uebersprungen += 1
			continue

		if _build(f, neuer_name, out_path):
			erzeugt += 1
		else:
			uebersprungen += 1

	print("---")
	if DRY_RUN:
		print("DRY_RUN = true -> nichts geschrieben. %d Szenen waeren erzeugt worden, %d uebersprungen."
			% [erzeugt, uebersprungen])
		print("Zum Schreiben DRY_RUN auf false setzen und erneut ausfuehren.")
	else:
		print("Fertig: %d Szenen in %s, %d uebersprungen." % [erzeugt, OUT_DIR, uebersprungen])
		EditorInterface.get_resource_filesystem().scan()


func _build(file_name: String, neuer_name: String, out_path: String) -> bool:
	var ps := load(SRC_DIR + file_name) as PackedScene
	if ps == null:
		push_warning("%s: laesst sich nicht laden" % file_name)
		return false

	var quelle: Node = ps.instantiate()
	var meshes: Array[MeshInstance3D] = []
	_collect(quelle, meshes)
	if meshes.is_empty():
		push_warning("%s: kein Mesh gefunden" % file_name)
		quelle.free()
		return false

	# Gesamt-AABB im Raum der Quell-Wurzel (die .glb-Wurzel hat oft Rotation/Skalierung
	# aus der Y-up-Umrechnung - die muss mit, sonst liegt der Fels falsch herum).
	var wurzel_xf: Transform3D = (quelle as Node3D).global_transform if quelle is Node3D else Transform3D.IDENTITY
	var gesamt := AABB()
	var erste := true
	var xforms: Array[Transform3D] = []
	for mi in meshes:
		var xf: Transform3D = wurzel_xf.affine_inverse() * mi.global_transform
		xforms.append(xf)
		if mi.mesh == null:
			continue
		var box := xf * mi.mesh.get_aabb()
		if erste:
			gesamt = box
			erste = false
		else:
			gesamt = gesamt.merge(box)

	var groesse := gesamt.size
	var max_kante: float = maxf(groesse.x, maxf(groesse.y, groesse.z))
	var versatz := Vector3.ZERO
	if ALIGN_BASE_TO_ZERO:
		versatz.y = -gesamt.position.y

	var art := "keine"
	if max_kante >= TRIMESH_AB:
		art = "trimesh"
	elif max_kante >= CONVEX_AB:
		art = "konvex"

	var tris := 0
	for mi in meshes:
		if mi.mesh != null:
			for s in mi.mesh.get_surface_count():
				var arr: Array = mi.mesh.surface_get_arrays(s)
				if not arr.is_empty() and arr[Mesh.ARRAY_INDEX] != null:
					tris += (arr[Mesh.ARRAY_INDEX] as PackedInt32Array).size() / 3

	print("  %-24s -> %-22s %5.1f x %5.1f x %5.1f m  %6d tris  Kollision: %s"
		% [file_name, neuer_name, groesse.x, groesse.y, groesse.z, tris, art])

	if DRY_RUN:
		quelle.free()
		return true

	# --- Szene bauen ---
	var wurzel: Node3D
	if art == "keine":
		wurzel = Node3D.new()
	else:
		wurzel = StaticBody3D.new()
	wurzel.name = neuer_name

	for i in meshes.size():
		var quell_mi := meshes[i]
		if quell_mi.mesh == null:
			continue
		var xf: Transform3D = xforms[i]
		xf.origin += versatz

		var neu_mi := MeshInstance3D.new()
		neu_mi.name = "Mesh" if meshes.size() == 1 else "Mesh%d" % i
		neu_mi.mesh = quell_mi.mesh
		neu_mi.transform = xf
		# Materialueberschreibungen des Originals mitnehmen.
		for s in quell_mi.get_surface_override_material_count():
			neu_mi.set_surface_override_material(s, quell_mi.get_surface_override_material(s))
		wurzel.add_child(neu_mi)
		neu_mi.owner = wurzel

		if art == "keine":
			continue

		var form: Shape3D
		if art == "trimesh":
			form = quell_mi.mesh.create_trimesh_shape()
		else:
			# simplify = true: der Konvexkoerper kommt mit deutlich weniger Ebenen aus.
			form = quell_mi.mesh.create_convex_shape(true, true)
		if form == null:
			push_warning("%s: Kollisionsform konnte nicht erzeugt werden" % neuer_name)
			continue

		var cs := CollisionShape3D.new()
		cs.name = "CollisionShape3D" if meshes.size() == 1 else "CollisionShape3D%d" % i
		cs.shape = form
		cs.transform = xf
		wurzel.add_child(cs)
		cs.owner = wurzel

	quelle.free()

	var packed := PackedScene.new()
	var err := packed.pack(wurzel)
	if err == OK:
		err = ResourceSaver.save(packed, out_path)
	wurzel.free()

	if err != OK:
		push_error("%s: Speichern fehlgeschlagen (Fehler %d)" % [neuer_name, err])
		return false
	return true


func _collect(node: Node, out: Array[MeshInstance3D]) -> void:
	if node is MeshInstance3D:
		out.append(node as MeshInstance3D)
	for c in node.get_children():
		_collect(c, out)
