extends MeshInstance3D
## Meldet die Spielerposition an den Barrieren-Shader. Dort tauchen daraufhin die kleinen
## Kreuze genau da auf, wo der Spieler steht, und wandern mit ihm mit
## (siehe barrier.gdshader, Gruppe "spieler_reaktion").
##
## Der Spieler wird ueber die Gruppe "player" gefunden - normalerweise nichts einzustellen.

## Optional: Spieler direkt angeben. Leer = Gruppe "player".
@export var player_path: NodePath

var _player: Node3D
var _mat: ShaderMaterial
var _last_sent: Vector3 = Vector3(1e9, 1e9, 1e9)


func _ready() -> void:
	_mat = get_active_material(0) as ShaderMaterial
	if _mat == null:
		push_error("Barriere: an Flaeche 0 haengt kein ShaderMaterial.")
		set_process(false)
		return
	_find_player()


func _process(_delta: float) -> void:
	if _player == null or not is_instance_valid(_player):
		_find_player()
		return
	var p: Vector3 = _player.global_position
	# Nur bei echter Bewegung schicken - spart den Uniform-Upload im Stillstand.
	if p.distance_squared_to(_last_sent) < 0.01:
		return
	_last_sent = p
	_mat.set_shader_parameter("player_position", p)


func _find_player() -> void:
	_player = get_node_or_null(player_path) as Node3D
	if _player == null:
		_player = get_tree().get_first_node_in_group("player") as Node3D
