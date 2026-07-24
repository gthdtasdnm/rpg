extends MeshInstance3D

## Der Spieler. Sendet beim Schwimmen Ringwellen (Kielwelle) aus.
## Leer -> zur Laufzeit ueber Gruppe "player" oder einen Node namens "Player" gesucht.
@export var player: Node3D

## Abstand zwischen zwei ausgesandten Wellen (Sekunden). Kleiner = dichtere Kielwelle.
@export var emit_interval: float = 0.07
## Lebensdauer jeder Welle (Sekunden). Kurz -> jeder Punkt schlaegt nur ein paar Wellen.
@export var ripple_duration: float = 1.2

# Muss RIPPLE_COUNT in water.gdshader entsprechen.
const RIPPLE_COUNT: int = 48

var _mat: ShaderMaterial
var _centers: PackedVector2Array = PackedVector2Array()
var _ages: PackedFloat32Array = PackedFloat32Array()
var _head: int = 0
var _emit_timer: float = 0.0


func _ready() -> void:
	_mat = get_active_material(0) as ShaderMaterial
	if _mat == null:
		push_error("Wasser: kein ShaderMaterial an Surface 0.")
		return
	if not player:
		player = get_tree().get_first_node_in_group("player")
	if not player:
		var found: Node = get_tree().get_current_scene().find_child("Player", true, false)
		if found is Node3D:
			player = found
	_centers.resize(RIPPLE_COUNT)
	_ages.resize(RIPPLE_COUNT)
	for i in RIPPLE_COUNT:
		_centers[i] = Vector2.ZERO
		_ages[i] = ripple_duration + 1.0  # startet inaktiv


func _process(delta: float) -> void:
	if _mat == null or player == null:
		return

	# Alle Wellen altern lassen (laufen weiter, auch wenn der Spieler das Wasser verlaesst).
	for i in RIPPLE_COUNT:
		_ages[i] += delta

	# Solange der Spieler unter der Oberflaeche ist: periodisch neue Welle an seiner Position.
	var below: bool = player.global_position.y < global_position.y
	if below:
		_emit_timer -= delta
		if _emit_timer <= 0.0:
			_emit_timer = emit_interval
			_head = (_head + 1) % RIPPLE_COUNT
			_centers[_head] = Vector2(player.global_position.x, player.global_position.z)
			_ages[_head] = 0.0

	_mat.set_shader_parameter("ripple_centers", _centers)
	_mat.set_shader_parameter("ripple_ages", _ages)
	_mat.set_shader_parameter("ripple_duration", ripple_duration)
