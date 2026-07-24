extends MeshInstance3D

@export var flow_speed: Vector2 = Vector2(0.03, 0.015)

var _mat: StandardMaterial3D


func _ready() -> void:
	var m: Material = get_active_material(0)
	if m == null:
		push_error("Wasser: kein Material an Surface 0 gefunden.")
		return
	if m is not StandardMaterial3D:
		push_error("Wasser: Material ist kein StandardMaterial3D, sondern " + m.get_class())
		return
	# Eigene Kopie, damit nur dieses Wasser bewegt wird.
	_mat = m.duplicate()
	set_surface_override_material(0, _mat)


func _process(delta: float) -> void:
	if _mat:
		_mat.uv1_offset += Vector3(flow_speed.x, flow_speed.y, 0.0) * delta
