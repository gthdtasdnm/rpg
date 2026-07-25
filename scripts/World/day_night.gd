@tool
extends Node3D
## Tageszeit-Steuerung: dreht die Sonne, fuettert die Sonnenrichtung an den Sky-Shader und
## passt Licht/Umgebung/Nebel an. Nachts uebernimmt der Mond eine schwache Beleuchtung.
## An den Environment-Node haengen (Sonne + WorldEnvironment werden automatisch gefunden).

## Uhrzeit (0-24). 6 = Sonnenaufgang, 12 = Mittag, 18 = Sonnenuntergang.
@export_range(0.0, 24.0, 0.05) var time_of_day: float = 10.0:
	set(value):
		time_of_day = value
		_apply()

@export_group("Sonne / Mond")
@export var day_light_color: Color = Color(1.0, 0.97, 0.9)
@export var dawn_light_color: Color = Color(1.0, 0.5, 0.22)
@export var max_sun_energy: float = 1.0
@export var moon_light_color: Color = Color(0.6, 0.7, 1.0)
@export var moon_energy: float = 0.15

@export_group("Nebel")
@export var fog_day: Color = Color(0.42, 0.5, 0.6)
@export var fog_dawn: Color = Color(0.5, 0.33, 0.26)
@export var fog_night: Color = Color(0.02, 0.03, 0.07)

var _sun: DirectionalLight3D
var _we: WorldEnvironment


func _ready() -> void:
	_resolve()
	_apply()


func _resolve() -> void:
	if _sun == null or _we == null:
		for c in get_children():
			if _sun == null and c is DirectionalLight3D:
				_sun = c
			elif _we == null and c is WorldEnvironment:
				_we = c


func _get_sky_material() -> ShaderMaterial:
	if _we != null and _we.environment != null and _we.environment.sky != null:
		return _we.environment.sky.sky_material as ShaderMaterial
	return null


func _apply() -> void:
	if not is_inside_tree():
		return
	_resolve()
	if _sun == null:
		return

	# Sonnenbahn: 6h = Horizont Ost, 12h = hoch (aber nicht senkrecht), 18h = Horizont West.
	var a: float = (time_of_day - 6.0) / 12.0 * PI
	var sun_dir: Vector3 = Vector3(cos(a), sin(a) * 0.9, -0.45).normalized()
	var elev: float = sun_dir.y

	# Sonnenrichtung an den Sky-Shader geben.
	var sky_mat: ShaderMaterial = _get_sky_material()
	if sky_mat != null:
		sky_mat.set_shader_parameter("sun_direction", sun_dir)

	var day: float = clampf(elev * 4.0, 0.0, 1.0)
	var dusk: float = clampf(1.0 - abs(elev) * 3.0, 0.0, 1.0)

	if elev > 0.0:
		# Tag/Daemmerung: Licht = Sonne.
		_sun.look_at_from_position(Vector3.ZERO, -sun_dir, Vector3.UP)
		_sun.light_energy = lerpf(0.1, max_sun_energy, day)
		_sun.light_color = dawn_light_color.lerp(day_light_color, day)
	else:
		# Nacht: Licht = Mond (gegenueber der Sonne, von oben), schwach und blaeulich.
		var moon_dir: Vector3 = -sun_dir
		_sun.look_at_from_position(Vector3.ZERO, -moon_dir, Vector3.UP)
		_sun.light_energy = moon_energy
		_sun.light_color = moon_light_color
	_sun.visible = true

	if _we != null and _we.environment != null:
		var env: Environment = _we.environment
		env.ambient_light_energy = lerpf(0.25, 1.0, day)
		var fog_col: Color = fog_night.lerp(fog_day, day)
		fog_col = fog_col.lerp(fog_dawn, dusk * 0.6)
		env.fog_light_color = fog_col
