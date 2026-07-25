extends GPUParticles3D
## Umgebungspartikel, die um den Spieler schweben. Tagsueber dezenter Staub/Pollen,
## nachts leuchtende Gluehwuermchen. Die Tag/Nacht-Umschaltung kommt vom day_night-Script
## (Gruppe "day_night", Property night_factor).

@export var player_path: NodePath
@export var follow_offset: Vector3 = Vector3(0.0, 3.0, 0.0)

@export var day_color: Color = Color(0.95, 0.95, 0.85)
@export var day_alpha: float = 0.5

@export_group("Nacht (Gluehpunkte, mischt mit den echten Fireflies)")
@export var night_color: Color = Color(0.8, 1.0, 0.45)
@export var night_alpha: float = 0.6
@export var night_glow: float = 2.5

var _player: Node3D
var _dn: Node
var _mat: StandardMaterial3D


func _ready() -> void:
	_player = get_node_or_null(player_path) as Node3D
	if _player == null:
		_player = get_tree().get_first_node_in_group("player") as Node3D
	_dn = get_tree().get_first_node_in_group("day_night")
	_mat = material_override as StandardMaterial3D
	# Sofort an den Spieler setzen, damit das Preprocess die Partikel gleich am richtigen Ort fuellt.
	if _player != null:
		global_position = _player.global_position + follow_offset


func _process(_delta: float) -> void:
	# Spieler ggf. nachtraeglich finden (falls die Szene vor dem Spieler geladen wurde).
	if _player == null:
		_player = get_tree().get_first_node_in_group("player") as Node3D
	# Emitter folgt dem Spieler (Partikel selbst leben im Weltraum -> ziehen natuerlich nach).
	if _player != null:
		global_position = _player.global_position + follow_offset

	if _mat == null:
		return
	if _dn == null:
		_dn = get_tree().get_first_node_in_group("day_night")  # spaet gefunden ist ok
	var night: float = 0.0
	if _dn != null:
		var nf: Variant = _dn.get("night_factor")
		if nf != null:
			night = nf
	# Tag: Staub. Nacht: schwache Gluehpunkte (mischen sich mit den echten Fireflies).
	var col: Color = day_color.lerp(night_color, night)
	col.a = lerpf(day_alpha, night_alpha, night)
	_mat.albedo_color = col
	_mat.emission = night_color
	_mat.emission_energy_multiplier = night * night_glow
