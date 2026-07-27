@tool
extends EditorScript

# Liest aus, was der Terrain3D-Instancer den erzeugten MultiMeshInstance3D-Knoten wirklich an
# LOD-/Fade-Einstellungen verpasst. Reine Diagnose, aendert nichts.
#
# WARUM DAS NOETIG IST:
# "fade_margin" am Terrain3DMeshAsset ist nur ein Wunsch. Ob daraus ein weicher Uebergang wird,
# haengt daran, ob Terrain3D am Knoten auch "visibility_range_fade_mode" setzt. Bei fade_mode = 0
# (DISABLED) ignoriert Godot die Margins komplett - dann poppt es hart, egal welcher fade_margin
# eingetragen ist. Genau das wollen wir hier sehen statt raten.
#
# BENUTZUNG:
#   1. Main.tscn (oder World/world.tscn) im Editor geoeffnet haben.
#   2. Diese Datei im Script-Editor oeffnen.
#   3. Menue "Datei" -> "Ausfuehren" (Strg+Umschalt+X).
#   4. Ausgabe im Panel "Ausgabe" kopieren.

## Nur MMIs ausgeben, deren Name diesen Text enthaelt (leer = alle).
const NAME_FILTER := ""
## Hoechstens so viele Knoten ausgeben (die Karte hat sehr viele Zellen).
const MAX_ROWS := 40

const FADE_NAMES := ["DISABLED (hart!)", "SELF", "DEPENDENCIES"]


func _run() -> void:
	var scene: Node = get_scene()
	if scene == null:
		push_error("Keine Szene offen.")
		return

	var terrain: Node = _find_terrain(scene)
	if terrain == null:
		push_error("Kein Terrain3D-Node in der offenen Szene gefunden.")
		return

	var mmis: Array[MultiMeshInstance3D] = []
	_find_mmis(terrain, mmis)

	print("=== Terrain3D-Instancer: %d MultiMeshInstance3D-Knoten ===" % mmis.size())
	print("Legende: begin/end = visibility_range, (+x) = zugehoeriger Margin")
	print("")

	# Zaehlen, welche fade_mode-Werte ueberhaupt vorkommen - das ist die eigentliche Antwort.
	var mode_count := {}
	var rows := 0

	for m in mmis:
		var mode: int = m.visibility_range_fade_mode
		mode_count[mode] = int(mode_count.get(mode, 0)) + 1

		if rows >= MAX_ROWS:
			continue
		if NAME_FILTER != "" and String(m.name).findn(NAME_FILTER) == -1:
			continue
		rows += 1

		var mesh_name := "-"
		if m.multimesh != null and m.multimesh.mesh != null:
			mesh_name = m.multimesh.mesh.get_class()
			if m.multimesh.mesh.resource_name != "":
				mesh_name = m.multimesh.mesh.resource_name

		var count := 0
		if m.multimesh != null:
			count = m.multimesh.instance_count

		var aabb: AABB = m.get_aabb()
		var center: Vector3 = m.global_transform * aabb.get_center()

		print("%-26s n=%-4d begin=%.0f(+%.0f) end=%.0f(+%.0f)  fade=%s  parent=%s" % [
			str(m.name), count,
			m.visibility_range_begin, m.visibility_range_begin_margin,
			m.visibility_range_end, m.visibility_range_end_margin,
			_fade_name(m.visibility_range_fade_mode),
			"ja" if m.visibility_parent != NodePath() else "nein",
		])
		print("%-26s   AABB-Mitte=(%.0f, %.0f, %.0f)  AABB-Groesse=(%.0f, %.0f, %.0f)  Mesh=%s" % [
			"", center.x, center.y, center.z,
			aabb.size.x, aabb.size.y, aabb.size.z, mesh_name,
		])

	print("")
	print("=== Verteilung der fade_mode-Werte ueber ALLE %d Knoten ===" % mmis.size())
	for mode in mode_count:
		print("  %-22s : %d Knoten" % [_fade_name(int(mode)), mode_count[mode]])

	# Der entscheidende Teil: WELCHE Knoten stehen auf DISABLED?
	# Ein Knoten mit begin > 0 ist eine Fern-Stufe (Imposter). Steht der auf DISABLED, poppt der
	# Imposter hart herein, waehrend der Baum darunter weich ausblendet - genau das Symptom.
	# Ein Knoten mit begin = 0 und end = 0 hat gar keinen Sichtbereich (Steine/Baumstuempfe ohne
	# LOD) und ist harmlos.
	print("")
	print("=== Kreuztabelle: fade_mode x Sichtbereich ===")
	var cross := {}
	for m in mmis:
		var kind: String
		if m.visibility_range_begin > 0.0 and m.visibility_range_end > 0.0:
			kind = "Mittelstufe (begin>0, end>0)"
		elif m.visibility_range_begin > 0.0:
			kind = "FERN-Stufe / Imposter (begin>0, end=0)"
		elif m.visibility_range_end > 0.0:
			kind = "NAH-Stufe / Vollmesh (begin=0, end>0)"
		else:
			kind = "ohne Sichtbereich (begin=0, end=0)"
		var key: String = "%s  |  %s" % [_fade_name(m.visibility_range_fade_mode), kind]
		cross[key] = int(cross.get(key, 0)) + 1
	var keys := cross.keys()
	keys.sort()
	for k in keys:
		print("  %-58s : %d" % [k, cross[k]])

	# Konkrete Beispiele der DISABLED-Knoten, damit klar ist, was sie sind.
	print("")
	print("=== Bis zu 12 Beispiele der DISABLED-Knoten ===")
	var shown := 0
	for m in mmis:
		if m.visibility_range_fade_mode != GeometryInstance3D.VISIBILITY_RANGE_FADE_DISABLED:
			continue
		if shown >= 12:
			break
		shown += 1
		var count := 0
		if m.multimesh != null:
			count = m.multimesh.instance_count
		var tris := 0
		if m.multimesh != null and m.multimesh.mesh != null:
			tris = m.multimesh.mesh.get_faces().size() / 3
		print("  %-26s n=%-4d begin=%.0f(+%.0f) end=%.0f(+%.0f)  Dreiecke/Instanz=%d  Pfad=%s" % [
			str(m.name), count,
			m.visibility_range_begin, m.visibility_range_begin_margin,
			m.visibility_range_end, m.visibility_range_end_margin,
			tris, str(m.get_path()).right(60),
		])


func _fade_name(mode: int) -> String:
	return FADE_NAMES[mode] if mode >= 0 and mode < FADE_NAMES.size() else "unbekannt(%d)" % mode


func _find_terrain(node: Node) -> Node:
	if node.get_class() == "Terrain3D":
		return node
	for c in node.get_children(true):
		var r: Node = _find_terrain(c)
		if r != null:
			return r
	return null


func _find_mmis(node: Node, acc: Array[MultiMeshInstance3D]) -> void:
	if node is MultiMeshInstance3D:
		acc.append(node)
	for c in node.get_children(true):
		_find_mmis(c, acc)
