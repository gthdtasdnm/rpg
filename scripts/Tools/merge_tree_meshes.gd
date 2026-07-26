@tool
extends EditorScript

# Fuehrt bei jedem Baummodell die getrennten Meshes (Stamm + Blaetter) zu EINEM Mesh mit mehreren
# Surfaces zusammen. Grund: Terrain3D deutet mehrere MeshInstance3D-Knoten faelschlich als
# LOD-Stufen (nah = Stamm, fern = Blaetter). Nach dem Zusammenfuehren sieht Terrain3D EIN Mesh
# und zeigt Stamm + Blaetter immer gemeinsam.
#
# BENUTZUNG:
#   1. Diese Datei im Godot-Script-Editor oeffnen.
#   2. Menue "Datei" -> "Ausfuehren" (Strg+Umschalt+X).
#   3. Ergebnis landet in res://Assets/Nature/Forest/merged/ (Originale bleiben unangetastet).
#   4. Im Terrain3D-Meshes-Dock jedem Baum-Asset die neue Szene aus merged/ zuweisen
#      (oder Bescheid geben, dann trage ich die Pfade in assets.tres ein).
#
# Zum Testen erst EIN Modell: ONLY_FILE unten setzen.

const SRC_DIR := "res://Assets/Nature/Forest/"
const OUT_DIR := "res://Assets/Nature/Forest/merged/"
const ONLY_FILE := ""  # z.B. "apple_tree_0.tscn" zum Testen, "" = alle


func _run() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var da := DirAccess.open(SRC_DIR)
	if da == null:
		push_error("Quellordner nicht gefunden: " + SRC_DIR)
		return
	var done := 0
	for f in da.get_files():
		if not f.ends_with(".tscn"):
			continue
		if ONLY_FILE != "" and f != ONLY_FILE:
			continue
		var ps := load(SRC_DIR + f) as PackedScene
		if ps == null:
			continue
		var root: Node = ps.instantiate()
		var mis: Array = []
		_collect(root, mis)
		if mis.size() <= 1:
			root.free()
			continue  # nur ein Mesh (z.B. Fels) -> nichts zu tun
		var combined := ArrayMesh.new()
		for mi in mis:
			var m: Mesh = mi.mesh
			if m == null:
				continue
			var xf: Transform3D = mi.transform
			for s in m.get_surface_count():
				var arrays: Array = m.surface_get_arrays(s)
				if xf != Transform3D.IDENTITY:
					_bake_transform(arrays, xf)
				# WICHTIG: den Primitive-Typ des Originals uebernehmen (die Baeume nutzen
				# Triangle-Strips, nicht Triangle-Listen) - sonst passt der Index-Puffer nicht.
				combined.add_surface_from_arrays(m.surface_get_primitive_type(s), arrays)
				var idx := combined.get_surface_count() - 1
				var mat: Material = mi.get_active_material(s)
				if mat == null:
					mat = m.surface_get_material(s)
				combined.surface_set_material(idx, mat)
		var out_mi := MeshInstance3D.new()
		out_mi.name = String(root.name)
		out_mi.mesh = combined
		var scene := PackedScene.new()
		scene.pack(out_mi)
		var out_path := OUT_DIR + f
		var err := ResourceSaver.save(scene, out_path)
		out_mi.free()
		root.free()
		if err == OK:
			done += 1
			print("Zusammengefuehrt: ", f, "  (", combined.get_surface_count(), " Surfaces)")
		else:
			push_error("Speichern fehlgeschlagen: " + out_path + " (Fehler " + str(err) + ")")
	print("Fertig. ", done, " Baummodelle -> ", OUT_DIR)
	EditorInterface.get_resource_filesystem().scan()


func _bake_transform(arrays: Array, xf: Transform3D) -> void:
	var verts: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
	for i in verts.size():
		verts[i] = xf * verts[i]
	arrays[Mesh.ARRAY_VERTEX] = verts
	if arrays[Mesh.ARRAY_NORMAL] != null:
		var norms: PackedVector3Array = arrays[Mesh.ARRAY_NORMAL]
		for i in norms.size():
			norms[i] = (xf.basis * norms[i]).normalized()
		arrays[Mesh.ARRAY_NORMAL] = norms
	if arrays[Mesh.ARRAY_TANGENT] != null:
		var tan: PackedFloat32Array = arrays[Mesh.ARRAY_TANGENT]
		for i in range(0, tan.size(), 4):
			var t := Vector3(tan[i], tan[i + 1], tan[i + 2])
			t = (xf.basis * t).normalized()
			tan[i] = t.x
			tan[i + 1] = t.y
			tan[i + 2] = t.z
		arrays[Mesh.ARRAY_TANGENT] = tan


func _collect(node: Node, out: Array) -> void:
	if node is MeshInstance3D:
		out.append(node)
	for c in node.get_children():
		_collect(c, out)
