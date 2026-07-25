# CLAUDE.md – Schnelleinstieg für KI-Assistenten

Godot-Projekt (RPG). Diese Datei zuerst lesen, dann bei Bedarf `doc/fortschritt-2026-07-25.md`
(Detailstand). Ziel: schnell orientiert, keine Inkonsistenzen, wenig Token verbrannt.

## Engine / Stack
- **Godot 4.7**, **.NET/C#** aktiviert, **Forward+**.
- Addons: **Terrain3D** (GDExtension, macOS/Win/Linux-Binaries dabei), limboai, dialogue_manager.
- Sprache mit dem User: **Deutsch**.

## ⚠️ Wichtigste Sache zuerst: es gibt ZWEI Ebenen
1. **Aktiv – die World/-Sandbox** (alles, was aktuell läuft/gebaut wird): Terrain, Himmel, Wasser,
   Gras, Bäume, Partikel, und ein **GDScript-Player**.
2. **Verwaist – das alte C#-RPG-Framework**: `scripts/` (Characters, Combat, Dialogue, Quests,
   Magic, Items, …) + `Data/` (JSON). Die **Szenen dazu wurden gelöscht** (Level/, Characters/*.tscn,
   Maps/ …). Es kompiliert noch, wird aber von der aktiven Szene **nicht** benutzt.
   → **`scripts/Characters/Player.cs` ist NICHT der aktive Player.** Aktiv ist
   **`scripts/World/player.gd`** (GDScript).
   → Alte Docs (`doc/architektur.md`, `doc/konzept/…`, `doc/anforderungen.md`) beschreiben dieses
   **alte** Framework und sind bzgl. Szenen/Struktur **veraltet**.

## Einstieg / Szenen
- **Hauptszene: `Main.tscn`** (Wurzel) → instanziert `World/world.tscn` (Terrain-Welt) +
  `World/player.tscn` (Player).
- `World/world.tscn` = Terrain3D + Gras + Wasser + Bäume/Instancer + Kollision + Partikel + Environment.

## Aktive Systeme (World/) und ihre Dateien
- **Player**: `World/player.tscn` + `scripts/World/player.gd` (WASD, Maus-3rd-Person, Shift=rennen,
  Springen, Schwimmen, Kamera-Clamp über Wasser). In Gruppe `player`.
- **Tag/Nacht + Himmel**: `World/Environment.tscn`, `scripts/World/day_night.gd` (Regler `Time Of Day`,
  stellt `night_factor` bereit), `World/sky.gdshader` (Sonne/Mond/Sterne/Wolken).
- **Wasser**: `Objects/water.gdshader` + `Objects/wasser.gd` (Tiefe/Refraktion/Reflexion + Kielwellen).
- **Gras**: `addons/terrain_3d/extras/particle_example/` (`grass.gdshader`, `terrain_3D_particles.gd`).
- **Terrain-Boden-Shader**: `World/terrain_shader.gdshader` (als `shader_override` am Terrain3D-Material;
  dunkelt Boden unter Gras ab, teilt `grass_noise.tres` mit dem Gras).
- **Bäume**: Assets `World/data/assets.tres` (Instancer). Modelle zu Einzel-Mesh zusammengeführt in
  `Models/Various_Forest_Assets_Pack/merged/` (Tool: `scripts/Tools/merge_tree_meshes.gd`).
  Kollision: `scripts/World/tree_collision.gd` (Laufzeit-Pool, filtert nach skalierter Höhe).
- **Partikel**: `scripts/World/firefly_swarm.gd` (Glühwürmchen = echte OmniLights, nachts),
  `scripts/World/forest_dust.gd` (grüner Staub bei Sträuchern, distanz-basiert).
  (`World/forest_particles.tscn` = alte GPU-Variante, ersetzt.)

## Terrain3D-Eigenheiten (oft gebraucht)
- Instancer erzeugt **nur Optik (MultiMesh), keine Kollision** → daher `tree_collision.gd`.
- **Kein Getter** für Instanz-Transforms → wir lesen die `MultiMeshInstance3D` unter dem Terrain-Node.
- Baummodelle waren Stamm+Blätter als **getrennte Meshes** (Terrain3D las sie als LODs) → deshalb `merged/`.
- Textur-Slots/LOD-Ranges: über das **Meshes-Asset-Dock** bzw. `World/data/assets.tres`.

## Arbeits-/Workflow-Hinweise (Zeit sparen, Fehler vermeiden)
- **Godot überschreibt `.tscn` beim Speichern.** Nach Datei-Edits den User **neu laden** lassen.
  Bei großen `.tscn` vorsichtig editieren (vorher lesen). Shader/`.gd` sind unkritisch.
- **Editor-RAM ≠ Spiel-Performance.** Für Perf immer FPS/ms im **laufenden Spiel** messen, nicht %.
- **Git**: Der lokale Stand divergiert oft vom Remote (User rebast/reset lokal). Etablierter Weg:
  `git push --force-with-lease origin main` (nach Absprache – der User bestätigt "einfach pushen").
- **Commits/Push nur auf Anweisung.** Commit-Message-Footer: `Co-Authored-By: Claude Opus 4.8 …`.
- **Leistung ist Thema**: GPU-Last hoch (~86 % auf RTX 2080 Ti), Ziel u. a. Laptops. Teuerste Posten:
  volumetrischer Nebel, Anzahl OmniLights (Glühwürmchen), Gras, Wasser-Shader, Schatten-/Baumdistanz.

## Aufräum-Reste (dürfen weg)
`_zum_loeschen/`, `demo/` (falls noch da), `Models/terrain_3d.tscn` (alt),
`World/forest_particles.tscn` (ersetzt).

## Detail-Stand
`doc/fortschritt-2026-07-25.md` – ausführliche Beschreibung aller Systeme + Stellschrauben + TODO.
