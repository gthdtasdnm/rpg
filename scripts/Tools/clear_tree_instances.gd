@tool
extends EditorScript

## Loescht Instancer-Instanzen (Baeume/Straeucher) auf einen Schlag,
## ohne die Mesh-Assets selbst aus der Liste zu entfernen.
##
## Benutzung: World/world.tscn (oder Main.tscn) im Editor oeffnen,
## dann Datei -> Ausfuehren (Strg+Umschalt+X) auf dieser Datei.
##
## VORHER die Terrain-Daten sichern (git commit oder Kopie von World/data/).
## Rueckgaengig gibt es nicht.

## Leer = alle Mesh-Assets loeschen.
## Sonst nur diese Asset-IDs, z. B. [0, 3, 7] (IDs stehen im Meshes-Asset-Dock).
const MESH_IDS: Array[int] = []

## true = nur zaehlen und ausgeben, nichts loeschen. Zum Nachsehen erst so laufen lassen.
const DRY_RUN: bool = true


func _run() -> void:
	var root := get_scene()
	var terrain := root.find_child("Terrain3D", true, false) as Terrain3D
	if terrain == null:
		push_error("Kein Terrain3D-Node in der offenen Szene gefunden.")
		return

	var assets := terrain.assets
	if assets == null:
		push_error("Terrain3D hat keine Assets.")
		return

	var ids: Array[int] = MESH_IDS
	if ids.is_empty():
		for i in assets.get_mesh_count():
			ids.append(i)

	print("--- Instanzen vor dem Loeschen ---")
	var vorher := _count_instances(terrain)
	for id in ids:
		var mesh_asset := assets.get_mesh_asset(id)
		var mesh_name := mesh_asset.get_name() if mesh_asset != null else "?"
		print("  ID %d: %s" % [id, mesh_name])
	print("  Instanzen gesamt in der Szene: %d" % vorher)

	if DRY_RUN:
		print("DRY_RUN = true -> nichts geloescht. Auf false setzen und erneut ausfuehren.")
		return

	for id in ids:
		terrain.instancer.clear_by_mesh(id)

	terrain.data.save_directory(terrain.data_directory)
	print("Geloescht. Instanzen jetzt: %d" % _count_instances(terrain))
	print("Terrain-Daten gespeichert nach: %s" % terrain.data_directory)


func _count_instances(terrain: Terrain3D) -> int:
	var summe := 0
	for mmi in terrain.find_children("*", "MultiMeshInstance3D", true, false):
		var m := mmi as MultiMeshInstance3D
		if m.multimesh != null:
			summe += m.multimesh.instance_count
	return summe
