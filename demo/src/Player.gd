extends CharacterBody3D

@export var MOVE_SPEED: float = 50.0
@export var JUMP_SPEED: float = 2.0
@export var first_person: bool = false : 
	set(p_value):
		first_person = p_value
		if first_person:
			var tween: Tween = create_tween()
			tween.tween_property($CameraManager/Arm, "spring_length", 0.0, .33)
			tween.tween_callback($Body.set_visible.bind(false))
		else:
			$Body.visible = true
			create_tween().tween_property($CameraManager/Arm, "spring_length", 6.0, .33)

@export var gravity_enabled: bool = true :
	set(p_value):
		gravity_enabled = p_value
		if not gravity_enabled:
			velocity.y = 0
			
@export var collision_enabled: bool = true :
	set(p_value):
		collision_enabled = p_value
		$CollisionShapeBody.disabled = ! collision_enabled
		$CollisionShapeRay.disabled = ! collision_enabled

@export_group("Swimming")
## Ab dieser globalen Y-Hoehe haelt sich der Spieler an der Wasseroberflaeche.
@export var water_level: float = -3.2
## Horizontales Tempo im Wasser.
@export var swim_speed: float = 25.0
## Vorwaerts-Neigung des Koerpers beim Schwimmen (Grad).
@export var swim_tilt_degrees: float = 75.0
## Zusaetzlicher Yaw-Offset, falls das Modell nicht exakt nach -Z blickt (Grad).
@export var swim_yaw_offset_degrees: float = 0.0
## Wie weit er auf/ab wippt (Meter).
@export var swim_bob_amplitude: float = 0.12
## Wippfrequenz.
@export var swim_bob_speed: float = 2.0
## Ab dieser Hoehe ueber water_level endet das Schwimmen wieder.
@export var swim_exit_height: float = 0.6

var _is_swimming: bool = false
var _swim_blend: float = 0.0  # 0 = aufrecht, 1 = schwimmend geneigt
var _bob_time: float = 0.0
var _body_yaw: float = 0.0  # Blickrichtung des Koerpers beim Schwimmen


func _physics_process(p_delta) -> void:
	var direction: Vector3 = get_camera_relative_input()
	var h_veloc: Vector2 = Vector2(direction.x, direction.z).normalized() * MOVE_SPEED
	if Input.is_key_pressed(KEY_SHIFT):
		h_veloc *= 2
	velocity.x = h_veloc.x
	velocity.z = h_veloc.y
	_update_swimming(p_delta)
	if not _is_swimming and gravity_enabled:
		velocity.y -= 40 * p_delta
	move_and_slide()


# Schwimmen: an der Wasseroberflaeche halten (nicht unter water_level sinken), leicht auf/ab
# wippen und den Koerper nach vorne neigen. Hysterese, damit der Zustand am Wasserrand nicht
# flackert.
func _update_swimming(p_delta: float) -> void:
	# Yaw, damit der Koerper genau in Kamerablickrichtung zeigt (Vorwaerts = -Z).
	var cam_basis: Basis = %Camera3D.global_transform.basis
	var cam_yaw: float = atan2(cam_basis.z.x, cam_basis.z.z) + deg_to_rad(swim_yaw_offset_degrees)

	if not _is_swimming and global_position.y < water_level:
		_is_swimming = true
		_body_yaw = cam_yaw # beim Eintauchen direkt ausrichten -> keine Drehung
	elif _is_swimming and global_position.y > water_level + swim_exit_height:
		_is_swimming = false

	if _is_swimming:
		_bob_time += p_delta * swim_bob_speed
		# Zielhoehe = Oberflaeche + sanftes Wippen; weich (Feder) dorthin ziehen und die
		# Steiggeschwindigkeit deckeln, damit man beim Reinfallen nicht rausgeschossen wird.
		var target_y: float = water_level + sin(_bob_time) * swim_bob_amplitude
		velocity.y = clampf((target_y - global_position.y) * 6.0, -8.0, 8.0)
		# Horizontales Tempo im Wasser begrenzen.
		var h_dir: Vector2 = Vector2(velocity.x, velocity.z)
		if h_dir.length() > swim_speed:
			h_dir = h_dir.normalized() * swim_speed
			velocity.x = h_dir.x
			velocity.z = h_dir.y
		# Koerper exakt an die Kamera ausrichten (direkt, ohne Nachlauf).
		_body_yaw = cam_yaw

	# Nur die Neigung wird ein-/ausgeblendet. Die Yaw wird NICHT mit _swim_blend skaliert,
	# sonst dreht sich der Koerper beim Ein-/Austauchen einmal komplett durch.
	_swim_blend = move_toward(_swim_blend, 1.0 if _is_swimming else 0.0, p_delta * 4.0)
	$Body.rotation = Vector3(deg_to_rad(swim_tilt_degrees) * _swim_blend, _body_yaw, 0.0)


# Returns the input vector relative to the camera. Forward is always the direction the camera is facing
func get_camera_relative_input() -> Vector3:
	var input_dir: Vector3 = Vector3.ZERO
	if Input.is_key_pressed(KEY_A): # Left
		input_dir -= %Camera3D.global_transform.basis.x
	if Input.is_key_pressed(KEY_D): # Right
		input_dir += %Camera3D.global_transform.basis.x
	if Input.is_key_pressed(KEY_W): # Forward
		input_dir -= %Camera3D.global_transform.basis.z
	if Input.is_key_pressed(KEY_S): # Backward
		input_dir += %Camera3D.global_transform.basis.z
	if Input.is_key_pressed(KEY_E) or Input.is_key_pressed(KEY_SPACE): # Up
		velocity.y += JUMP_SPEED + MOVE_SPEED*.016
	if Input.is_key_pressed(KEY_Q): # Down
		velocity.y -= JUMP_SPEED + MOVE_SPEED*.016
	if Input.is_key_pressed(KEY_KP_ADD) or Input.is_key_pressed(KEY_EQUAL):
		MOVE_SPEED = clamp(MOVE_SPEED + .5, 5, 9999)
	if Input.is_key_pressed(KEY_KP_SUBTRACT) or Input.is_key_pressed(KEY_MINUS):
		MOVE_SPEED = clamp(MOVE_SPEED - .5, 5, 9999)
	return input_dir


func _input(p_event: InputEvent) -> void:
	if p_event is InputEventMouseButton and p_event.pressed:
		if p_event.button_index == MOUSE_BUTTON_WHEEL_UP:
			MOVE_SPEED = clamp(MOVE_SPEED + 5, 5, 9999)
		elif p_event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			MOVE_SPEED = clamp(MOVE_SPEED - 5, 5, 9999)
	
	elif p_event is InputEventKey:
		if p_event.pressed:
			if p_event.keycode == KEY_V:
				first_person = ! first_person
			elif p_event.keycode == KEY_G:
				gravity_enabled = ! gravity_enabled
			elif p_event.keycode == KEY_C:
				collision_enabled = ! collision_enabled

		# Else if up/down released
		elif p_event.keycode in [ KEY_Q, KEY_E, KEY_SPACE ]:
			velocity.y = 0
