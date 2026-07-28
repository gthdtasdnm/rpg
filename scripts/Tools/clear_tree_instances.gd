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
var mesh_ids: Array[int] = []

## true = nur zaehlen und ausgeben, nichts loeschen. Zum Nachsehen erst so laufen lassen.
const DRY_RUN: bool = false


func _run() -> void:
	var root := get_scene()
	if root == null:
		push_error("Keine Szene offen. World/world.tscn oder Main.tscn oeffnen.")
		return

	var terrain := root.find_child("Terrain3D", true, false) as Terrain3D
	if terrain == null:
		push_error("Kein Terrain3D-Node in der offenen Szene gefunden.")
		return

	var assets := terrain.assets
	if assets == null:
		push_error("Terrain3D hat keine Assets.")
		return

	# .duplicate(), sonst zeigt ids auf ein evtl. read-only Array.
	var ids: Array[int] = mesh_ids.duplicate()
	if ids.is_empty():
		var anzahl := assets.get_mesh_count()
		print("assets.get_mesh_count() = %d" % anzahl)
		if anzahl <= 0:
			# Fallback, falls get_mesh_count() nichts liefert
			var liste: Array = assets.get_mesh_list()
			anzahl = liste.size()
			print("Fallback ueber get_mesh_list(): %d Eintraege" % anzahl)
		for i in anzahl:
			ids.append(i)

	if ids.is_empty():
		push_error("Keine Mesh-Assets gefunden. Ist das Meshes-Asset-Dock gefuellt?")
		return

	print("--- Instanzen vor dem Loeschen ---")
	for id in ids:
		var mesh_asset: Resource = assets.get_mesh_asset(id)
		var mesh_name := "?"
		if mesh_asset != null:
			mesh_name = str(mesh_asset.get("name"))
			if mesh_name == "" or mesh_name == "<null>":
				mesh_name = mesh_asset.resource_name
		print("  ID %d: %s" % [id, mesh_name])
	print("  Instanzen gesamt in der Szene: %d" % _count_instances(terrain))

	if DRY_RUN:
		print("DRY_RUN = true -> nichts geloescht. Auf false setzen und erneut ausfuehren.")
		return

	var instancer := terrain.instancer
	if instancer == null:
		push_error("Terrain3D hat keinen Instancer.")
		return

	for id in ids:
		instancer.clear_by_mesh(id)

	terrain.data.save_directory(terrain.data_directory)
	print("Geloescht. Instanzen jetzt: %d" % _count_instances(terrain))
	print("Terrain-Daten gespeichert nach: %s" % terrain.data_directory)
	print("Falls im Viewport noch Baeume stehen: Szene neu laden (die MMIs haengen noch im Baum).")


func _count_instances(terrain: Terrain3D) -> int:
	var summe := 0
	for mmi in terrain.find_children("*", "MultiMeshInstance3D", true, false):
		var m := mmi as MultiMeshInstance3D
		if m.multimesh != null:
			summe += m.multimesh.instance_count
	return summe
