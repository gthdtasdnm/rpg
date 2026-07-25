extends Node3D
## Umgebungs-Staub nach dem Gluehwuermchen-Prinzip: kleine Partikel, die FEST bei Straeuchern/Busch-
## Instanzen in der Welt sitzen und nur im Umkreis um den Spieler sichtbar sind (weiches Ein-/Aus-
## blenden nach Distanz). Sie ziehen NICHT mit dem Spieler mit und poppen nicht vor ihm auf.

@export var terrain_path: NodePath
@export var count: int = 45
@export var radius: float = 100.0            # bis hierhin sichtbar
@export var fade_start: float = 70.0         # ab hier nach aussen ausblenden
## Bei Instanzen mit (skalierter) Hoehe in diesem Bereich platzieren -> Straeucher/Buesche.
@export var place_min_height: float = 0.3
@export var place_max_height: float = 3.5
@export var mote_size: float = 0.09
@export var color: Color = Color(0.55, 0.9, 0.45)
@export var alpha: float = 0.6
@export var drift_speed: float = 0.3
@export var refresh_interval: float = 1.5
@export var max_samples: int = 400
## Nur tagsueber zeigen? Aus = immer sichtbar.
@export var day_only: bool = false

class Mote:
	var root: Node3D
	var mat: StandardMaterial3D
	var vel: Vector3
	var life: float = 0.0

var _player: Node3D
var _dn: Node
var _terrain: Node3D
var _motes: Array[Mote] = []
var _spots: PackedVector3Array = PackedVector3Array()
var _refresh_t: float = 0.0


func _ready() -> void:
	_player = get_tree().get_first_node_in_group("player") as Node3D
	_dn = get_tree().get_first_node_in_group("day_night")
	_terrain = get_node_or_null(terrain_path) as Node3D
	if _terrain == null:
		_terrain = _find_terrain(get_tree().get_current_scene())
	_refresh_spots()
	for i in count:
		var m: Mote = _make_mote()
		_place(m)
		m.life = randf()
		_motes.append(m)
	print("ForestDust: %d Partikel, %d Busch-Positionen gefunden (Terrain: %s)."
		% [_motes.size(), _spots.size(), "ja" if _terrain != null else "NEIN"])


func _center() -> Vector3:
	return _player.global_position if _player != null else global_position


func _make_mote() -> Mote:
	var m: Mote = Mote.new()
	m.root = Node3D.new()
	add_child(m.root)
	var mesh: MeshInstance3D = MeshInstance3D.new()
	var qm: QuadMesh = QuadMesh.new()
	qm.size = Vector2(mote_size, mote_size)
	mesh.mesh = qm
	m.mat = StandardMaterial3D.new()
	m.mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	m.mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	m.mat.billboard_keep_scale = true
	m.mat.albedo_color = color
	mesh.material_override = m.mat
	m.root.add_child(mesh)
	return m


func _place(m: Mote) -> void:
	var p: Vector3
	if _spots.size() > 0:
		var base: Vector3 = _spots[randi() % _spots.size()]
		p = base + Vector3(randf_range(-1.5, 1.5), randf_range(0.2, 1.8), randf_range(-1.5, 1.5))
	else:
		var c: Vector3 = _center()
		var ang: float = randf() * TAU
		var d: float = sqrt(randf()) * radius
		p = c + Vector3(cos(ang) * d, randf_range(0.5, 3.0), sin(ang) * d)
	m.root.global_position = p
	m.vel = Vector3(randf() - 0.5, (randf() - 0.5) * 0.4, randf() - 0.5).normalized() * drift_speed
	m.life = 0.0


func _refresh_spots() -> void:
	_spots = PackedVector3Array()
	if _terrain == null:
		return
	var c: Vector3 = _center()
	var mmis: Array[MultiMeshInstance3D] = []
	_find_mmis(_terrain, mmis)
	for mmi in mmis:
		var mm: MultiMesh = mmi.multimesh
		if mm == null or mm.mesh == null:
			continue
		var mh: float = mm.mesh.get_aabb().size.y
		var gx: Transform3D = mmi.global_transform
		for i in mm.instance_count:
			var wt: Transform3D = gx * mm.get_instance_transform(i)
			var eh: float = mh * wt.basis.get_scale().y
			if eh < place_min_height or eh > place_max_height:
				continue
			var p: Vector3 = wt.origin
			if Vector2(p.x - c.x, p.z - c.z).length() <= radius:
				_spots.append(p)
				if _spots.size() >= max_samples:
					return


func _process(delta: float) -> void:
	if _player == null:
		_player = get_tree().get_first_node_in_group("player") as Node3D
	if _dn == null:
		_dn = get_tree().get_first_node_in_group("day_night")
	_refresh_t -= delta
	if _refresh_t <= 0.0:
		_refresh_t = refresh_interval
		_refresh_spots()

	var c: Vector3 = _center()
	var day: float = 1.0
	if day_only and _dn != null:
		var nf: Variant = _dn.get("night_factor")
		if nf != null:
			day = 1.0 - float(nf)

	for m in _motes:
		m.root.global_position += m.vel * delta
		var d: float = Vector2(m.root.global_position.x - c.x, m.root.global_position.z - c.z).length()
		var vis: float = 1.0 - smoothstep(fade_start, radius, d)
		m.life = move_toward(m.life, vis * day, 2.0 * delta)
		if d > radius + 5.0 and m.life <= 0.01:
			_place(m)
		var col: Color = color
		col.a = alpha * m.life
		m.mat.albedo_color = col


func _find_mmis(node: Node, acc: Array[MultiMeshInstance3D]) -> void:
	if node is MultiMeshInstance3D:
		acc.append(node)
	for ch in node.get_children(true):
		_find_mmis(ch, acc)


func _find_terrain(node: Node) -> Node3D:
	if node == null:
		return null
	if node.get_class() == "Terrain3D":
		return node as Node3D
	for ch in node.get_children():
		var r: Node3D = _find_terrain(ch)
		if r != null:
			return r
	return null
