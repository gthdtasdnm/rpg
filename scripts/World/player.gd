extends CharacterBody3D
## Eigenstaendiger Spieler: 3rd-Person-Kamera (Maus), WASD relativ zur Kamera, Springen,
## Schwerkraft und Schwimmen (an der Wasseroberflaeche halten, wippen, nach vorne neigen).
## Liegt in Gruppe "player", damit Gras-Interaktion und Wasser-Wellen ihn automatisch finden.

@export_group("Movement")
@export var speed: float = 12.0
@export var acceleration: float = 50.0
@export var rotation_speed: float = 12.0
@export var jump_velocity: float = 8.0
@export var gravity: float = 24.0
@export var sprint_multiplier: float = 1.8
@export var mouse_sensitivity: float = 0.003
@export var min_pitch_deg: float = -70.0
@export var max_pitch_deg: float = 60.0

@export_group("Swimming")
@export var water_level: float = -3.2
@export var swim_speed: float = 8.0
@export var swim_tilt_degrees: float = 70.0
@export var swim_bob_amplitude: float = 0.12
@export var swim_bob_speed: float = 2.0
@export var swim_exit_height: float = 0.6
## Hoehe der Wasseroberflaeche (Wassermesh). Die Kamera bleibt knapp darueber.
@export var water_surface_y: float = -2.0
## Sicherheitsabstand der Kamera ueber der Wasseroberflaeche.
@export var camera_water_margin: float = 0.4

var _yaw: float = 0.0
var _pitch: float = 0.0
var _is_swimming: bool = false
var _swim_blend: float = 0.0
var _bob_time: float = 0.0
var _cam_rest: Transform3D
var _dialogue_active: bool = false

@onready var _pivot: Node3D = $CameraPivot
@onready var _body: Node3D = $Body
@onready var _camera: Camera3D = $CameraPivot/Camera3D


func _ready() -> void:
	add_to_group("player")
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	_yaw = rotation.y
	_cam_rest = _camera.transform  # Ruhelage der Kamera merken

	# Waehrend eines Gespraechs steht der Spieler still. Das Signal kommt vom Autoload "Dialog"
	# (scripts/Dialogue/DialogueBridge.cs) - get_node_or_null, damit der Player auch in einer
	# Testszene ohne die Spielsysteme laeuft.
	var dialog := get_node_or_null("/root/Dialog")
	if dialog:
		dialog.connect("DialogueStarted", _on_dialogue_started)
		dialog.connect("DialogueEnded", _on_dialogue_ended)


func _on_dialogue_started() -> void:
	_dialogue_active = true


func _on_dialogue_ended() -> void:
	_dialogue_active = false


func _unhandled_input(event: InputEvent) -> void:
	if _dialogue_active:
		return

	# Escape gehoert dem HUD (Pause-Menue, siehe scripts/UI/Hud.cs) - der Player fasst die
	# Maussteuerung nicht mehr selbst an. Solange ein Panel offen ist, steht die Maus auf
	# "sichtbar", und die Abfrage unten sorgt dafuer, dass sich die Kamera dann nicht mitdreht.
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		_yaw -= event.relative.x * mouse_sensitivity
		_pitch -= event.relative.y * mouse_sensitivity
		_pitch = clampf(_pitch, deg_to_rad(min_pitch_deg), deg_to_rad(max_pitch_deg))


## Wird vom SaveSystem beim Laden aufgerufen: Position und Drehung setzen UND den intern
## gefuehrten Kamera-Yaw nachziehen - sonst springt die Kamera im naechsten Frame zurueck,
## weil _yaw noch den alten Wert haelt.
func apply_save_state(position: Vector3, rotation_y: float) -> void:
	global_position = position
	rotation = Vector3(0.0, rotation_y, 0.0)
	_yaw = rotation_y


func _physics_process(delta: float) -> void:
	var v: Vector3 = velocity

	# Im Gespraech: stehenbleiben, aber weiter der Schwerkraft folgen (sonst bleibt der Spieler
	# in der Luft haengen, wenn ein Dialog waehrend eines Sprungs startet).
	if _dialogue_active:
		v.x = 0.0
		v.z = 0.0
		if not is_on_floor():
			v.y -= gravity * delta
		velocity = v
		move_and_slide()
		return

	# Eingaberichtung relativ zur Kamera-Gierung.
	var input := Vector2.ZERO
	if Input.is_physical_key_pressed(KEY_W): input.y -= 1.0
	if Input.is_physical_key_pressed(KEY_S): input.y += 1.0
	if Input.is_physical_key_pressed(KEY_A): input.x -= 1.0
	if Input.is_physical_key_pressed(KEY_D): input.x += 1.0
	var dir: Vector3 = (Basis(Vector3.UP, _yaw) * Vector3(input.x, 0.0, input.y)).normalized()

	# Schwimm-Zustand (Hysterese).
	if not _is_swimming and global_position.y < water_level:
		_is_swimming = true
	elif _is_swimming and global_position.y > water_level + swim_exit_height:
		_is_swimming = false

	if _is_swimming:
		_bob_time += delta * swim_bob_speed
		var surface: float = water_level + sin(_bob_time) * swim_bob_amplitude
		# Sanfter Auftrieb Richtung Oberflaeche - als Beschleunigung, nicht hart gesetzt,
		# damit Kollisionen/Boden den Spieler an Kanten rausschieben koennen.
		var buoy: float = clampf((surface - global_position.y) * 4.0, -3.0, 3.0)
		v.y = move_toward(v.y, buoy, 25.0 * delta)
		# Leertaste: aktiv nach oben schwimmen / an der Kante rausklettern.
		if Input.is_physical_key_pressed(KEY_SPACE):
			v.y = swim_speed
		# Sobald Boden unter den Fuessen ist (flache Kante), Schwimmen beenden.
		if is_on_floor():
			_is_swimming = false
	else:
		if not is_on_floor():
			v.y -= gravity * delta
		if is_on_floor() and Input.is_physical_key_pressed(KEY_SPACE):
			v.y = jump_velocity

	var cur_speed: float = swim_speed if _is_swimming else speed
	if not _is_swimming and Input.is_physical_key_pressed(KEY_SHIFT):
		cur_speed *= sprint_multiplier  # Shift = rennen
	var target: Vector3 = dir * cur_speed
	v.x = move_toward(v.x, target.x, acceleration * delta)
	v.z = move_toward(v.z, target.z, acceleration * delta)

	velocity = v
	move_and_slide()

	# Koerper sanft in Bewegungsrichtung drehen.
	if dir != Vector3.ZERO:
		var ta: float = atan2(-dir.x, -dir.z)
		rotation.y = lerp_angle(rotation.y, ta, rotation_speed * delta)

	# Kamera nachfuehren (Yaw unabhaengig von der Koerperdrehung).
	_pivot.rotation = Vector3(_pitch, _yaw - rotation.y, 0.0)

	# Kamera jedes Frame auf ihre Ruhelage zuruecksetzen (sonst bleibt eine vorige Clamp-Korrektur
	# haengen und die Kamera kommt nicht mehr runter).
	_camera.transform = _cam_rest

	# Kamera nie unter die Wasserlinie: anheben UND wieder auf den Spieler ausrichten,
	# sonst zeigt sie ins Leere und der Drehpunkt wirkt verschoben. (Kein Tauchen gewollt.)
	var min_y: float = water_surface_y + camera_water_margin
	if _camera.global_position.y < min_y:
		var gp: Vector3 = _camera.global_position
		gp.y = min_y
		_camera.global_position = gp
		_camera.look_at(_pivot.global_position, Vector3.UP)

	# Schwimm-Neigung nur auf das sichtbare Mesh (Kollision/Kamera bleiben aufrecht).
	_swim_blend = move_toward(_swim_blend, 1.0 if _is_swimming else 0.0, delta * 4.0)
	_body.rotation.x = -deg_to_rad(swim_tilt_degrees) * _swim_blend
