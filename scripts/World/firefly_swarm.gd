extends Node3D
## Gluehwuermchen mit ECHTEM Licht (OmniLight3D). Wenige, in der Welt verteilte Lichter, die
## langsam driften und blinken. Driftet eins zu weit vom Spieler weg (oder wird es Tag), blendet
## es weich aus und wird am Rand des Umkreises neu platziert -> wirkt, als existierten sie in der
## Welt, statt vor dem Spieler zu spawnen. Nur nachts aktiv (night_factor vom day_night-Script).

@export var count: int = 14
@export var radius: float = 40.0            # Umkreis um den Spieler
@export var inner_radius: float = 5.0
@export var height_min: float = 0.5
@export var height_max: float = 4.0
@export var drift_speed: float = 0.5
@export var wander_interval: float = 1.5    # Sekunden bis Richtungswechsel
@export var color: Color = Color(0.85, 1.0, 0.5)
@export var light_range: float = 7.0
@export var light_energy: float = 1.0
@export var glow_energy: float = 2.5        # Eigenleuchten (Bloom) der Punkte
@export var fly_size: float = 0.08
@export var fade_speed: float = 1.0         # wie schnell ein-/ausblenden

class Fly:
	var root: Node3D
	var light: OmniLight3D
	var mat: StandardMaterial3D
	var vel: Vector3
	var life: float = 0.0
	var blink: float = 0.0
	var wander: float = 0.0

var _player: Node3D
var _dn: Node
var _flies: Array[Fly] = []


func _ready() -> void:
	_player = get_tree().get_first_node_in_group("player") as Node3D
	_dn = get_tree().get_first_node_in_group("day_night")
	var center: Vector3 = _center()
	for i in count:
		var f: Fly = _make_fly()
		_place(f, center, inner_radius / radius)   # ueber den ganzen Bereich verteilt (bereits da)
		f.life = randf()
		f.blink = randf() * TAU
		_flies.append(f)


func _center() -> Vector3:
	return _player.global_position if _player != null else global_position


func _make_fly() -> Fly:
	var f: Fly = Fly.new()
	f.root = Node3D.new()
	add_child(f.root)
	f.light = OmniLight3D.new()
	f.light.omni_range = light_range
	f.light.light_color = color
	f.light.light_energy = 0.0
	f.light.shadow_enabled = false
	f.root.add_child(f.light)
	var mesh: MeshInstance3D = MeshInstance3D.new()
	var qm: QuadMesh = QuadMesh.new()
	qm.size = Vector2(fly_size, fly_size)
	mesh.mesh = qm
	f.mat = StandardMaterial3D.new()
	f.mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	f.mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	f.mat.blend_mode = BaseMaterial3D.BLEND_MODE_ADD
	f.mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	f.mat.billboard_keep_scale = true
	f.mat.albedo_color = color
	f.mat.emission_enabled = true
	f.mat.emission = color
	mesh.material_override = f.mat
	f.root.add_child(mesh)
	return f


func _place(f: Fly, center: Vector3, near_frac: float) -> void:
	var ang: float = randf() * TAU
	var dist: float = lerpf(radius * near_frac, radius, sqrt(randf()))
	f.root.global_position = center + Vector3(cos(ang) * dist, 0.0, sin(ang) * dist)
	f.root.global_position.y = center.y + randf_range(height_min, height_max)
	f.vel = Vector3(randf() - 0.5, (randf() - 0.5) * 0.3, randf() - 0.5).normalized() * drift_speed


func _process(delta: float) -> void:
	if _player == null:
		_player = get_tree().get_first_node_in_group("player") as Node3D
	if _dn == null:
		_dn = get_tree().get_first_node_in_group("day_night")
	var night: float = 0.0
	if _dn != null:
		var nf: Variant = _dn.get("night_factor")
		if nf != null:
			night = nf
	var center: Vector3 = _center()

	for f in _flies:
		# Driften + gelegentlicher Richtungswechsel
		f.wander -= delta
		if f.wander <= 0.0:
			f.wander = wander_interval
			f.vel = (f.vel + Vector3(randf() - 0.5, (randf() - 0.5) * 0.3, randf() - 0.5) * drift_speed).limit_length(drift_speed)
		f.root.global_position += f.vel * delta

		# Zu weit weg oder Tag -> ausblenden; wenn ganz aus, am Rand neu platzieren.
		var d: float = Vector2(f.root.global_position.x - center.x, f.root.global_position.z - center.z).length()
		var want_alive: bool = night > 0.02 and d < radius + 3.0
		var target: float = 1.0 if want_alive else 0.0
		f.life = move_toward(f.life, target, fade_speed * delta)
		if f.life <= 0.001 and not want_alive and night > 0.02:
			_place(f, center, 0.65)   # nur am aeusseren Rand -> ploppt nicht vor dem Spieler auf

		# Blinken + Helligkeit
		f.blink += delta * 3.0
		var b: float = 0.55 + 0.45 * sin(f.blink)
		var e: float = f.life * night * b
		f.light.light_energy = e * light_energy
		f.mat.emission_energy_multiplier = e * glow_energy
		var col: Color = color
		col.a = f.life * night
		f.mat.albedo_color = col
