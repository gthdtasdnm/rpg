extends Node3D
## Laufzeit-Kollision fuer Terrain3D-Instanzen (Baeume/Felsen).
##
## Der Terrain3D-Instancer rendert nur Optik (MultiMesh) ohne Kollision. Dieses Script erzeugt
## Stamm-Kollider (Zylinder) NUR fuer Instanzen im Umkreis des Spielers und recycelt sie beim
## Weiterlaufen. Dadurch bleibt es egal, wie viele Baeume die Karte hat.
##
## Nutzung: als Kind der Szene einhaengen, "terrain_path" auf den Terrain3D-Node und
## "player_path" auf den Spieler setzen (oder Spieler in Gruppe "player").

## Der Terrain3D-Node (dessen MultiMeshInstance3D-Kinder werden ausgelesen).
@export var terrain_path: NodePath
## Der Spieler.
@export var player_path: NodePath
## Nur Instanzen innerhalb dieses Radius (m) um den Spieler bekommen einen Kollider.
@export var radius: float = 35.0
## Zylinder-Radius des Stammes.
@export var trunk_radius: float = 0.5
## Zylinder-Hoehe des Stammes.
@export var trunk_height: float = 6.0
## Neu berechnen, sobald der Spieler so viele Meter gelaufen ist (spart CPU).
@export var update_distance: float = 4.0
## Kollisions-Layer der erzeugten Koerper (muss zur Spieler-Maske passen).
@export_flags_3d_physics var collision_layer: int = 1
## Optionaler Namensfilter: nur MMIs, deren Name diesen Text enthaelt (leer = alle Instanzen).
@export var include_filter: String = ""
## Nur Instanzen, die (skaliert!) HOEHER als das sind, bekommen Kollision (m). So kollidieren
## Baeume, aber niedrige/kleinskalierte Straeucher/Saplings/Steine nicht.
@export var min_collision_height: float = 4.0
## Gibt beim ersten Aufbau die Anzahl gefundener MMIs/Kollider aus (zum Pruefen, danach aus).
@export var debug: bool = true

var _debug_done: bool = false

var _terrain: Node3D
var _player: Node3D
var _bodies: Array[StaticBody3D] = []
var _last_pos: Vector3 = Vector3(1e9, 1e9, 1e9)


func _ready() -> void:
	var scene: Node = get_tree().get_current_scene()
	# Terrain: erst ueber den Export, sonst per Klasse "Terrain3D" in der Szene suchen.
	_terrain = get_node_or_null(terrain_path) as Node3D
	if _terrain == null and scene != null:
		_terrain = _find_terrain(scene)
	# Player: Export -> Gruppe "player" -> Node namens "Player".
	_player = get_node_or_null(player_path) as Node3D
	if _player == null:
		_player = get_tree().get_first_node_in_group("player") as Node3D
	if _player == null and scene != null:
		_player = scene.find_child("Player", true, false) as Node3D
	if _terrain == null:
		push_error("TreeCollision: kein Terrain3D-Node gefunden.")
	if _player == null:
		push_error("TreeCollision: kein Player gefunden.")
	set_physics_process(_terrain != null and _player != null)


func _find_terrain(node: Node) -> Node3D:
	if node.get_class() == "Terrain3D":
		return node as Node3D
	for c in node.get_children():
		var r: Node3D = _find_terrain(c)
		if r != null:
			return r
	return null


func _physics_process(_delta: float) -> void:
	var pos: Vector3 = _player.global_position
	if _last_pos.distance_to(pos) < update_distance:
		return
	_last_pos = pos
	_rebuild(pos)


func _rebuild(center: Vector3) -> void:
	var points: Array[Vector3] = _gather(center)
	if debug and not _debug_done:
		_debug_done = true
		var all: Array[MultiMeshInstance3D] = []
		_find_mmis(_terrain, all)
		print("TreeCollision: %d MMIs, %d Kollider im Umkreis %.0fm (min_collision_height=%.1f):"
			% [all.size(), points.size(), radius, min_collision_height])
		for m in all:
			if m.multimesh != null and m.multimesh.mesh != null and m.multimesh.instance_count > 0:
				var base_h: float = m.multimesh.mesh.get_aabb().size.y
				var sc: float = (m.global_transform * m.multimesh.get_instance_transform(0)).basis.get_scale().y
				var h: float = base_h * sc
				var status: String = "KOLLISION" if h >= min_collision_height else "frei"
				print("  %s  ~%.2f m (base %.2f x scale %.2f)  -> %s" % [str(m.name), h, base_h, sc, status])
	while _bodies.size() < points.size():
		_bodies.append(_make_body())
	for i in _bodies.size():
		var b: StaticBody3D = _bodies[i]
		var cs: CollisionShape3D = b.get_child(0)
		if i < points.size():
			b.global_position = points[i]
			cs.disabled = false
		else:
			cs.disabled = true
			b.global_position = Vector3(0.0, -100000.0, 0.0)


func _gather(center: Vector3) -> Array[Vector3]:
	var out: Array[Vector3] = []
	var r2: float = radius * radius
	var mmis: Array[MultiMeshInstance3D] = []
	_find_mmis(_terrain, mmis)
	for mmi in mmis:
		var mm: MultiMesh = mmi.multimesh
		if mm == null or mm.instance_count == 0 or mm.mesh == null:
			continue
		var mesh_h: float = mm.mesh.get_aabb().size.y
		var gx: Transform3D = mmi.global_transform
		# Grober Regions-Skip: liegt die AABB der MMI ueberhaupt in Reichweite?
		var aabb: AABB = mmi.get_aabb()
		var wc: Vector3 = gx * aabb.get_center()
		if Vector2(wc.x - center.x, wc.z - center.z).length() > radius + aabb.size.length():
			continue
		for i in mm.instance_count:
			var wt: Transform3D = gx * mm.get_instance_transform(i)
			# Effektive (skalierte) Hoehe der Instanz - niedrige/kleinskalierte ueberspringen.
			if mesh_h * wt.basis.get_scale().y < min_collision_height:
				continue
			var p: Vector3 = wt.origin
			var dx: float = p.x - center.x
			var dz: float = p.z - center.z
			if dx * dx + dz * dz <= r2:
				out.append(p)
	return out


func _find_mmis(node: Node, acc: Array[MultiMeshInstance3D]) -> void:
	if node is MultiMeshInstance3D:
		if include_filter == "" or String(node.name).findn(include_filter) != -1:
			acc.append(node)
	for c in node.get_children(true):
		_find_mmis(c, acc)


func _make_body() -> StaticBody3D:
	var b: StaticBody3D = StaticBody3D.new()
	b.collision_layer = collision_layer
	b.collision_mask = 0  # Baeume kollidieren passiv, muessen selbst nichts detektieren
	var cs: CollisionShape3D = CollisionShape3D.new()
	var cyl: CylinderShape3D = CylinderShape3D.new()
	cyl.radius = trunk_radius
	cyl.height = trunk_height
	cs.shape = cyl
	cs.position = Vector3(0.0, trunk_height * 0.5, 0.0)  # Basis am Boden
	b.add_child(cs)
	add_child(b)
	return b
