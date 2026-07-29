# Copyright © 2023-2026 Cory Petkovsek, Roope Palmroos, and Contributors.
#
# This is an example of using a particle shader with Terrain3D.
# To use it, add `Terrain3DParticles.tscn` to your scene and assign the terrain.
# Then customize the settings, materials and shader to extend it and make it your own.

@tool
extends Node3D


#region settings
## Auto set if attached as a child of a Terrain3D node
@export var terrain: Terrain3D:
	set(value):
		terrain = value
		_create_grid()


## Rasterweite zwischen den Instanzen. Bestimmt die PARTIKELZAHL
## (amount = (cell_width / instance_spacing)^2) - der Regler fuer die Leistung.
## Zum Ausduennen der Optik NICHT hier drehen, sondern "dichte" im Prozess-Material:
## ein grobes Raster sieht man als Reihen, weil die Streuung nur ein Vielfaches der
## Rasterweite ist.
@export_range(0.125, 4.0, 0.015625) var instance_spacing: float = 0.5:
	set(value):
		instance_spacing = clamp(round(value * 64.0) * 0.015625, 0.125, 4.0)
		rows = maxi(int(cell_width / instance_spacing), 1)
		amount = rows * rows
		_aktualisiere_aabb()
		_sync_raster()
		_set_offsets()


## Width of an individual cell of the grid
@export_range(8.0, 256.0, 1.0) var cell_width: float = 32.0:
	set(value):
		cell_width = clamp(value, 8.0, 256.0)
		rows = maxi(int(cell_width / instance_spacing), 1)
		amount = rows * rows
		min_draw_distance = 1.0
		_aktualisiere_aabb()
		_sync_raster()
		_set_offsets()


## Grid width. Must be odd.
## Higher values cull slightly better, draw further out.
@export_range(1, 15, 2) var grid_width: int = 9:
	set(value):
		grid_width = value
		particle_count = 1
		min_draw_distance = 1.0
		_create_grid()


@export_storage var rows: int = 1

@export_storage var amount: int = 1:
	set(value):
		amount = value
		particle_count = value
		last_pos = Vector3.ZERO
		for p in particle_nodes:
			p.amount = amount


@export_range(1, 256, 1) var process_fixed_fps: int = 30:
	set(value):
		process_fixed_fps = maxi(value, 1)
		for p in particle_nodes:
			p.fixed_fps = process_fixed_fps
			p.preprocess = 1.0 / float(process_fixed_fps)


## Access to process material parameters
@export var process_material: ShaderMaterial

## The player node. Grass parts to the sides as it passes.
## If left empty, a node named "Player" is searched for at runtime.
@export var player: Node3D

## Horizontal radius (meters) of the parting around the player's path.
@export var trail_radius: float = 0.9

## How far the grass is pushed to the side.
@export var trail_push: float = 0.55

## Seconds a parting stays open before the grass springs back.
@export var trail_lifetime: float = 3.0

## Minimum distance the player must move before a new trail point is recorded.
@export var trail_point_spacing: float = 0.35

## The mesh that each particle will render
@export var mesh: Mesh

@export var shadow_mode: GeometryInstance3D.ShadowCastingSetting = (
		GeometryInstance3D.ShadowCastingSetting.SHADOW_CASTING_SETTING_ON):
	set(value):
		shadow_mode = value
		for p in particle_nodes:
			p.cast_shadow = value


## Override material for the particle mesh
@export_custom(
	PROPERTY_HINT_RESOURCE_TYPE,
	"BaseMaterial3D,ShaderMaterial") var mesh_material_override: Material:
	set(value):
		mesh_material_override = value
		for p in particle_nodes:
			p.material_override = mesh_material_override


@export_group("Info")
## The minimum distance that particles will be drawn upto
## If using fade out effects like pixel alpha this is the limit to use.
@export var min_draw_distance: float = 1.0:
	set(value):
		min_draw_distance = float(cell_width * grid_width) * 0.5


## Displays current total particle count based on Cell Width and Instance Spacing
@export var particle_count: int = 1:
	set(value):
		particle_count = amount * grid_width * grid_width

#endregion


var offsets: Array[Vector3]
var last_pos: Vector3 = Vector3.ZERO
var particle_nodes: Array[GPUParticles3D]

# Player trail. Must match TRAIL_SIZE in grass.gdshader.
const TRAIL_SIZE: int = 16
var _trail_pos: PackedVector3Array = PackedVector3Array()
var _trail_dir: PackedVector2Array = PackedVector2Array()
var _trail_age: PackedFloat32Array = PackedFloat32Array()
var _trail_strength: PackedFloat32Array = PackedFloat32Array()
var _trail_head: int = 0
var _last_trail_pos: Vector3 = Vector3.ZERO


func _ready() -> void:
	if not terrain:
		var parent: Node = get_parent()
		if parent is Terrain3D:
			terrain = parent
	# Auto-find the player by name at runtime if not assigned in the editor.
	if not player and not Engine.is_editor_hint():
		var found: Node = get_tree().get_current_scene().find_child("Player", true, false)
		if found is Node3D:
			player = found
	_init_trail()
	_create_grid()
	# Beim Laden der Szene laufen die Setter, bevor Terrain-Daten und Partikelknoten
	# bereitstehen. Hier nochmal, mit echten Werten.
	_aktualisiere_aabb()
	_sync_raster()
	# Beim Wiedereinblenden muss das Raster neu geschrieben werden - solange der Knoten
	# unsichtbar war, hat er das Material nicht angefasst (siehe _physics_process).
	if not visibility_changed.is_connected(_sync_raster):
		visibility_changed.connect(_sync_raster)
	_pruefe_geteiltes_material()


## Das Prozess-Material traegt das Raster EINES Knotens (instance_amount, instance_rows,
## instance_spacing). Zwei aktive Terrain3DParticles mit derselben Material-Ressource
## ueberschreiben sich deshalb gegenseitig jeden Frame - der Verlierer legt seine
## Partikel in ein fremdes Raster und malt statt einer Zelle nur einen Streifen davon.
## Das ist stundenlang als "Dichte laesst sich nicht einstellen" missverstanden worden,
## deshalb hier eine Warnung statt eines stillen Fehlers.
func _pruefe_geteiltes_material() -> void:
	if not process_material:
		return
	var wurzel: Node = get_tree().get_current_scene() if get_tree() else null
	if not wurzel:
		return
	for anderer in wurzel.find_children("*", "Node3D", true, false):
		if anderer == self or anderer.get_script() != get_script():
			continue
		if anderer.process_material == process_material and anderer.is_visible_in_tree():
			push_warning("Terrain3DParticles: %s und %s teilen sich dasselbe " %
					[get_path(), anderer.get_path()] +
					"process_material. Das Raster beider Knoten kollidiert - " +
					"eine Kopie des Materials zuweisen oder einen Knoten entfernen.")


func _init_trail() -> void:
	_trail_pos.resize(TRAIL_SIZE)
	_trail_dir.resize(TRAIL_SIZE)
	_trail_age.resize(TRAIL_SIZE)
	_trail_strength.resize(TRAIL_SIZE)
	for i in TRAIL_SIZE:
		_trail_pos[i] = Vector3.ZERO
		_trail_dir[i] = Vector2.ZERO
		# Start fully faded so no parting shows before the player moves.
		_trail_age[i] = trail_lifetime + 1.0
		_trail_strength[i] = 0.0
	if player:
		_last_trail_pos = player.global_position


func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		_destroy_grid()


func _physics_process(delta: float) -> void:
	# Unsichtbar heisst hier auch "haende weg vom Prozess-Material": ein ausgeblendeter
	# Knoten hat sonst weiter sein Raster hineingeschrieben und damit das Gras eines
	# ANDEREN Knotens zerlegt, ohne selbst etwas zu zeichnen.
	if not is_visible_in_tree():
		return
	if terrain:
		var camera: Camera3D = terrain.get_camera()
		if camera:
			if last_pos.distance_squared_to(camera.global_position) > 1.0:
				var pos: Vector3 = camera.global_position.snapped(Vector3.ONE)
				_position_grid(pos)
				RenderingServer.material_set_param(process_material.get_rid(), "camera_position", pos )
				last_pos = camera.global_position
		_update_process_parameters()
		# Update the player trail and feed it to the grass mesh shader.
		if player and mesh_material_override is ShaderMaterial:
			_update_trail(delta)
			var grass_mat: ShaderMaterial = mesh_material_override as ShaderMaterial
			grass_mat.set_shader_parameter("trail_positions", _trail_pos)
			grass_mat.set_shader_parameter("trail_dirs", _trail_dir)
			grass_mat.set_shader_parameter("trail_strength", _trail_strength)
			grass_mat.set_shader_parameter("trail_radius", trail_radius)
			grass_mat.set_shader_parameter("trail_push", trail_push)
	else:
		set_physics_process(false)


func _update_trail(delta: float) -> void:
	# Age every point and recompute its strength (1.0 fresh -> 0.0 sprung back).
	var inv_life: float = 1.0 / maxf(trail_lifetime, 0.001)
	for i in TRAIL_SIZE:
		_trail_age[i] += delta
		_trail_strength[i] = clampf(1.0 - _trail_age[i] * inv_life, 0.0, 1.0)
	# Record a new point once the player has moved far enough.
	var ppos: Vector3 = player.global_position
	var moved: Vector3 = ppos - _last_trail_pos
	moved.y = 0.0
	if moved.length() >= trail_point_spacing:
		var dir: Vector2 = Vector2(moved.x, moved.z).normalized()
		_trail_head = (_trail_head + 1) % TRAIL_SIZE
		_trail_pos[_trail_head] = ppos
		_trail_dir[_trail_head] = dir
		_trail_age[_trail_head] = 0.0
		_trail_strength[_trail_head] = 1.0
		_last_trail_pos = ppos


func _create_grid() -> void:
	_destroy_grid()
	if not terrain:
		return
	set_physics_process(true)
	_set_offsets()
	# Gleiche Berechnung wie oben - stand hier urspruenglich ein zweites Mal mit
	# demselben Vorzeichenfehler.
	var aabb: AABB = _berechne_aabb()
	var half_grid: int = grid_width / 2
	# Iterating the array like this allows identifying grid position, in case setting
	# different mesh or materials is desired for LODs etc.
	for x in range(-half_grid, half_grid + 1):
		for z in range(-half_grid, half_grid + 1):
			#var ring: int = maxi(maxi(absi(x), absi(z)), 0)
			var particle_node = GPUParticles3D.new()
			particle_node.lifetime = 600.0
			particle_node.amount = amount
			particle_node.explosiveness = 1.0
			particle_node.amount_ratio = 1.0
			particle_node.process_material = process_material
			particle_node.draw_pass_1 = mesh
			particle_node.speed_scale = 1.0
			particle_node.custom_aabb = aabb
			particle_node.cast_shadow = shadow_mode
			particle_node.fixed_fps = process_fixed_fps
			# This prevent minor grid alignment errors when the camera is moving very fast
			particle_node.preprocess = 1.0 / float(process_fixed_fps)
			if mesh_material_override:
				particle_node.material_override = mesh_material_override
			particle_node.use_fixed_seed = true
			if (x > -half_grid and z > -half_grid): # Use the same seed across all nodes
				particle_node.seed = particle_nodes[0].seed
			self.add_child(particle_node)
			particle_node.emitting = true
			particle_nodes.push_back(particle_node)
	last_pos = Vector3.ZERO


## Sichtbarkeitskoerper je Zelle. Muss die volle Gelaendehoehe umfassen, sonst
## verwirft Godot ganze Zellen und es entstehen Streifen.
##
## Die urspruengliche Fassung rechnete height_range[0] - height_range[1]. Da
## get_height_range() (min, max) liefert, war das NEGATIV, und der Ursprung lag
## zusaetzlich am oberen Rand statt am unteren - heraus kam ein entarteter Koerper
## der Hoehe 0.
## Schreibt das Partikelraster SOFORT ins Prozess-Material.
##
## Der Prozess-Shader legt die Instanzen als vec3(INDEX % instance_rows, 0,
## INDEX / instance_rows) an. Das ergibt nur dann ein Quadrat, wenn
## instance_rows == rows == cell_width / instance_spacing gilt.
##
## Vorher wurde das nur in _physics_process nachgezogen, waehrend im .tres ein
## alter Wert gespeichert blieb - beim Aendern der Dichte entstanden dadurch
## rechteckige Raster und damit leere Streifen. Jetzt haengt es direkt am Regler
## und wird mitgespeichert.
func _sync_raster() -> void:
	# Partikelzahl gehoert dem Knoten, das Raster dem (moeglicherweise geteilten)
	# Material - deshalb erst die Knoten, und ins Material nur, wenn wir auch zeichnen.
	for p in particle_nodes:
		p.amount = amount
	if not process_material or not is_visible_in_tree():
		return
	process_material.set_shader_parameter("instance_amount", amount)
	process_material.set_shader_parameter("instance_rows", rows)
	process_material.set_shader_parameter("instance_spacing", instance_spacing)
	var rid: RID = process_material.get_rid()
	if rid.is_valid():
		RenderingServer.material_set_param(rid, "instance_amount", amount)
		RenderingServer.material_set_param(rid, "instance_rows", rows)
		RenderingServer.material_set_param(rid, "instance_spacing", instance_spacing)
	# Die Partikel legen ihre Position beim Entstehen fest - ohne Neustart
	# behalten sie das alte Raster.
	for p in particle_nodes:
		p.amount = amount
		p.restart(true)
	# Zellen sofort neu ausrichten. _position_grid laeuft sonst erst, wenn sich die
	# Kamera um mehr als einen Meter bewegt hat - bis dahin stehen die Zellen noch
	# im alten Abstand und es klaffen Luecken.
	last_pos = Vector3(1e9, 1e9, 1e9)
	if terrain:
		var kamera: Camera3D = terrain.get_camera()
		if kamera:
			_position_grid(kamera.global_position.snapped(Vector3.ONE))


func _berechne_aabb() -> AABB:
	var unten: float = -50.0
	var hoehe: float = 400.0
	if terrain and terrain.data:
		var hr: Vector2 = terrain.data.get_height_range()
		var lo: float = minf(hr.x, hr.y)
		var hi: float = maxf(hr.x, hr.y)
		if hi - lo > 1.0:
			unten = lo - 5.0
			hoehe = (hi - lo) + 10.0      # Rand fuer Grashoehe und Windauslenkung
	# Rand fuer die Streuung: random_spacing = 1.0 schiebt einen Partikel bis zu einer
	# vollen Rasterweite aus seiner Zelle heraus. Ohne den Rand verschwindet die
	# Zelle am Bildrand, waehrend ihre aeussersten Bueschel noch sichtbar waeren.
	var zugabe: float = instance_spacing
	var breite: float = cell_width + zugabe * 2.0
	var aabb: AABB = AABB()
	aabb.size = Vector3(breite, hoehe, breite)
	aabb.position = Vector3(-breite * 0.5, unten, -breite * 0.5)
	return aabb


func _aktualisiere_aabb() -> void:
	var aabb: AABB = _berechne_aabb()
	for p in particle_nodes:
		p.custom_aabb = aabb


func _set_offsets() -> void:
	# Der Zellabstand MUSS exakt der Flaeche entsprechen, die eine Zelle bemalt.
	# Der Prozess-Shader legt sqrt(instance_amount) Reihen an - also muss hier
	# dieselbe Zahl stehen. Vorher stand hier "rows", das ist eine zweite,
	# getrennt gepflegte Groesse: lief sie auseinander, rueckten die Zellen weiter
	# auseinander als sie malten, und es entstanden leere Stellen dazwischen.
	var reihen: int = maxi(int(round(sqrt(float(amount)))), 1)
	var schritt: float = float(reihen) * instance_spacing
	var half_grid: int = grid_width / 2
	offsets.clear()
	for x in range(-half_grid, half_grid + 1):
		for z in range(-half_grid, half_grid + 1):
			offsets.append(Vector3(float(x) * schritt, 0.0, float(z) * schritt))


func _destroy_grid() -> void:
	for node: GPUParticles3D in particle_nodes:
		if is_instance_valid(node):
			node.queue_free()
	particle_nodes.clear()


func _position_grid(pos: Vector3) -> void:
	for i in particle_nodes.size():
		var node: GPUParticles3D = particle_nodes[i]
		var snap = Vector3(pos.x, 0, pos.z).snapped(Vector3.ONE) + offsets[i]
		node.global_position = (snap / instance_spacing).round() * instance_spacing
		node.reset_physics_interpolation()
		node.restart(true) # keep the same seed.


func _update_process_parameters() -> void:
	if process_material:
		var process_rid: RID = process_material.get_rid()
		if terrain and process_rid.is_valid():
			RenderingServer.material_set_param(process_rid, "_background_mode", terrain.material.world_background)
			RenderingServer.material_set_param(process_rid, "_vertex_spacing", terrain.vertex_spacing)
			RenderingServer.material_set_param(process_rid, "_vertex_density", 1.0 / terrain.vertex_spacing)
			RenderingServer.material_set_param(process_rid, "_region_size", terrain.region_size)
			RenderingServer.material_set_param(process_rid, "_region_texel_size", 1.0 / terrain.region_size)
			RenderingServer.material_set_param(process_rid, "_region_map_size", 32)
			RenderingServer.material_set_param(process_rid, "_region_map", terrain.data.get_region_map())
			RenderingServer.material_set_param(process_rid, "_region_locations", terrain.data.get_region_locations())
			RenderingServer.material_set_param(process_rid, "_height_maps", terrain.data.get_height_maps_rid())
			RenderingServer.material_set_param(process_rid, "_control_maps", terrain.data.get_control_maps_rid())
			RenderingServer.material_set_param(process_rid, "_color_maps", terrain.data.get_color_maps_rid())
			RenderingServer.material_set_param(process_rid, "instance_spacing", instance_spacing)
			RenderingServer.material_set_param(process_rid, "instance_amount", amount)
			RenderingServer.material_set_param(process_rid, "instance_rows", rows)
			RenderingServer.material_set_param(process_rid, "max_dist", min_draw_distance)
