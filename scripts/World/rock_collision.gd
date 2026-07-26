extends Node3D
## Gibt allen Steinen unter diesem Knoten beim Spielstart eine Kollision.
##
## Warum per Script statt beim Import: die Steine liegen schon fertig platziert in der Szene.
## So muss nichts neu importiert werden, und jeder Stein, den du später dazustellst, bekommt
## seine Kollision automatisch.
##
## ## Welche Form wofür?
##
## [b]TRIMESH[/b] folgt dem Mesh exakt - Mulden, Kanten, Löcher, alles. Man schwebt nicht und
## Durchgänge bleiben offen. Kostet Speicher (jedes Dreieck wird übernommen) und einmalig etwas
## Ladezeit; für [i]unbewegliche[/i] Objekte ist das aber der übliche Weg, weil die Physik-Engine
## daraus einen Suchbaum baut und im Betrieb nur wenige Dreiecke prüft.
##
## [b]CONVEX[/b] legt sich wie Schrumpffolie um den Stein: sehr billig, aber Vertiefungen werden
## aufgefüllt (man steht dann etwas über dem Boden) und Löcher zugeklebt.
##
## [b]DECOMPOSED[/b] zerlegt den Stein in mehrere konvexe Klötze - näher an der Form als CONVEX,
## billiger als TRIMESH. Achtung: die Zerlegung passiert beim Spielstart und ist bei so
## hochaufgelösten Steinen [i]spürbar langsam[/i]. Nur nehmen, wenn TRIMESH wirklich zu teuer ist.
##
## `Print Summary` einschalten zeigt nach dem Start in der Konsole, was es tatsächlich gekostet
## hat (Anzahl Formen, Dreiecke, Millisekunden) - lieber messen als raten.

enum ShapeMode {
	TRIMESH,          ## exakt, folgt dem Mesh (Standard)
	CONVEX,           ## Hülle, sehr billig, füllt Mulden und Löcher
	CONVEX_SIMPLIFIED,## noch gröbere Hülle
	DECOMPOSED,       ## mehrere konvexe Klötze, langsamer Start
}

## Aus = gar keine Kollision erzeugen (zum Vergleichen).
@export var enabled: bool = true

## Form für alle Steine, sofern kein Sonderfall unten greift.
@export var mode: ShapeMode = ShapeMode.TRIMESH

## Steine, die diesen Text im Namen haben, bekommen abweichend `override_mode`.
## Es reicht ein Teil des Namens ("tor" trifft "Rock_tor" und "rock_tor2").
@export var override_names: PackedStringArray = []

@export var override_mode: ShapeMode = ShapeMode.TRIMESH

## Steine, die gar keine Kollision bekommen sollen (z.B. reine Deko im Hintergrund).
@export var exclude_names: PackedStringArray = []

## Wie viele Klötze DECOMPOSED höchstens erzeugt. Mehr = genauer und langsamer.
@export_range(1, 64) var decompose_max_hulls: int = 8

## Notbremse: Meshes mit mehr Dreiecken als hier bekommen statt TRIMESH die billige Hülle.
## Ein einzelnes zu fein aufgelöstes Modell kann sonst Ladezeit und Speicher sprengen -
## Photogrammetrie-Rohdaten haben schnell über 100.000 Dreiecke pro Stein.
## 0 = Grenze aus (dann gilt immer `mode`).
@export var trimesh_triangle_limit: int = 20000

## Meldet nach dem Start in der Konsole, was die Kollision gekostet hat.
@export var print_summary: bool = false


func _ready() -> void:
	if not enabled:
		return

	var started_usec := Time.get_ticks_usec()
	var shapes := 0
	var triangles := 0
	var skipped := 0

	for rock in get_children():
		if _matches(rock.name, exclude_names):
			skipped += 1
			continue

		var rock_mode: ShapeMode = override_mode if _matches(rock.name, override_names) else mode
		var result := _add_collision_to(rock, rock_mode)
		shapes += result.x
		triangles += result.y

	if print_summary:
		var elapsed_ms := (Time.get_ticks_usec() - started_usec) / 1000.0
		print("[rock_collision] %d Formen, %d Dreiecke, %d übersprungen — %.1f ms"
			% [shapes, triangles, skipped, elapsed_ms])


func _matches(node_name: String, patterns: PackedStringArray) -> bool:
	var lower := node_name.to_lower()
	for pattern in patterns:
		if pattern != "" and lower.contains(pattern.to_lower()):
			return true
	return false


## Haengt an jedes Mesh unterhalb von `node` einen StaticBody3D. Der Koerper wird Kind des
## Meshes - dadurch erbt er dessen Position, Drehung und Skalierung, ohne Rechnerei hier.
## Rueckgabe: (Anzahl Formen, Anzahl Dreiecke) - nur fuer die Statistik.
func _add_collision_to(node: Node, rock_mode: ShapeMode) -> Vector2i:
	var shapes := 0
	var triangles := 0

	for mesh_instance in _find_meshes(node):
		var mesh := mesh_instance.mesh
		if mesh == null:
			continue

		var mesh_mode := rock_mode
		if mesh_mode == ShapeMode.TRIMESH and trimesh_triangle_limit > 0:
			var tris := mesh.get_faces().size() / 3
			if tris > trimesh_triangle_limit:
				mesh_mode = ShapeMode.CONVEX
				push_warning("rock_collision: '%s' hat %d Dreiecke (Grenze %d) — Hülle statt Trimesh. Mesh vereinfachen."
					% [mesh_instance.name, tris, trimesh_triangle_limit])

		var forms := _build_shapes(mesh, mesh_mode)
		if forms.is_empty():
			push_warning("rock_collision: keine Form für '%s'." % mesh_instance.name)
			continue

		var body := StaticBody3D.new()
		body.name = "Collision"

		for form in forms:
			var collision := CollisionShape3D.new()
			collision.shape = form
			body.add_child(collision)
			shapes += 1

		mesh_instance.add_child(body)

		if mesh_mode == ShapeMode.TRIMESH:
			triangles += mesh.get_faces().size() / 3

	return Vector2i(shapes, triangles)


func _build_shapes(mesh: Mesh, rock_mode: ShapeMode) -> Array[Shape3D]:
	var forms: Array[Shape3D] = []

	match rock_mode:
		ShapeMode.TRIMESH:
			var trimesh := mesh.create_trimesh_shape()
			if trimesh != null:
				forms.append(trimesh)

		ShapeMode.CONVEX:
			var hull := mesh.create_convex_shape(true, false)
			if hull != null:
				forms.append(hull)

		ShapeMode.CONVEX_SIMPLIFIED:
			var simple := mesh.create_convex_shape(true, true)
			if simple != null:
				forms.append(simple)

		ShapeMode.DECOMPOSED:
			var settings := MeshConvexDecompositionSettings.new()
			settings.max_convex_hulls = decompose_max_hulls
			for piece in mesh.convex_decompose(settings):
				forms.append(piece)

	return forms


func _find_meshes(node: Node) -> Array[MeshInstance3D]:
	var found: Array[MeshInstance3D] = []

	if node is MeshInstance3D:
		found.append(node)

	for child in node.get_children():
		found.append_array(_find_meshes(child))

	return found
