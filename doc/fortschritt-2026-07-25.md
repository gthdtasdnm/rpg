# Fortschritt – 2026-07-25

Zusammenfassung der heute umgesetzten Systeme, mit Dateien und wichtigen Stellschrauben.

---

## Projektstruktur / Aufräumen
- **Hauptszene: `Main.tscn`** (Wurzel) → lädt `World/world.tscn` + `World/player.tscn`.
- Terrain-Welt unter **`World/`** (`World/world.tscn`, `World/data/` = Terrain3D-Daten + `assets.tres`).
- Von der Terrain3D-**Demo entkoppelt** (Player/UI/DemoScene entfernt, Daten/Texturen verschoben). `demo/` ist löschbar.
- **Gepackte Texturen** in `PackedTextures/` (Albedo+Height / NormalGL+Roughness, 1024², **BPTC**). Roh-Sets in `_zum_loeschen/`.
- ⚠️ Altes C#-RPG-Framework (`scripts/`, `Data/`) ist **verwaist** (Szenen gelöscht) – siehe `CLAUDE.md`.

## Spieler – `World/player.tscn` + `scripts/World/player.gd`
Eigenständiger Player (ersetzt den Demo-Player):
- 3rd-Person-Kamera (Maus), WASD kamerarelativ, **Shift = rennen**, Springen, Schwerkraft.
- **Schwimmen**: hält an der Wasseroberfläche, wippt, neigt sich nach vorne (`water_level`, `swim_*`).
- **Kamera-Clamp** über der Wasserlinie (`water_surface_y`), damit unter Wasser nichts kaputtgeht.
- In Gruppe `player` (Gras/Wasser/Partikel finden ihn automatisch).

## Gras (`addons/terrain_3d/extras/particle_example/`)
- **Spieler-Trail**: Gras wird beim Durchlaufen zur Seite geteilt, Spur bleibt kurz offen (`grass.gdshader` + `terrain_3D_particles.gd`).
- Wind wird im geteilten Pfad ausgeblendet; Halme biegen sich (Wurzel fix).
- Fake-AO am Halmgrund (`base_ao`).
- Wächst nur auf bestimmten Textur-IDs (nicht 2–20).

## Terrain-Boden – `World/terrain_shader.gdshader` (Shader-Override)
- Dunkelt den Boden dort ab, wo (hohes) Gras wächst – gleiche `main_noise` wie das Gras (geteilt: `grass_noise.tres`), Hangfilter, Textur-ID-Filter.

## Wasser – `Objects/water.gdshader` + `Objects/wasser.gd`
- Tiefenabhängige Farbe (dunkler = tiefer), **Refraktion**, **Reflexion** (Fresnel).
- **Kielwelle beim Schwimmen**: der Spieler sendet periodisch Ringwellen aus (Array von Wellen, blinken/verlaufen). Nur Normalen, nicht Refraktion (sonst Artefakte am Spieler/Rand → `clamp`).

## Bäume (Terrain3D-Instancer, `World/data/assets.tres`)
- Baummodelle waren Stamm+Blätter als **getrennte Meshes** → Terrain3D las sie als LODs (Blätter/Stamm getrennt sichtbar). **Fix**: per Tool-Script `scripts/Tools/merge_tree_meshes.gd` zu **Einzel-Mesh** zusammengeführt (`merged/`), Assets zeigen darauf, `last_lod = 0`.
- **Specular** auf allen Baum-Materialien aus (`specular_mode`) → kein weißes Gegenlicht-Aufleuchten.
- Sicht-/Cull-Distanz: `lod0_range` (aktuell 500). Nebel dahinter.
- **Laufzeit-Kollision**: `scripts/World/tree_collision.gd` – Zylinder-Kollider nur im Umkreis des Spielers (wandernder Pool). Filtert nach **effektiver (skalierter) Höhe** (`min_collision_height`), damit Sträucher nicht kollidieren.

## Tag/Nacht + Himmel – `World/Environment.tscn`
- **Regler `Time Of Day`** (0–24) am Environment-Node (`scripts/World/day_night.gd`): dreht die Sonne, setzt Licht/Umgebung/Nebel, nachts Mondlicht; stellt `night_factor` bereit.
- **Sky-Shader** `World/sky.gdshader`: prozeduraler Himmel-Gradient, Sonne + Halo, **Mond**, **Sterne**, ziehende **Wolken** (vom Sonnenlicht durchleuchtet). Sonnenrichtung kommt vom Script.
- Nebel = **Depth-Fog** (beginnt erst in der Ferne). God Rays über volumetrischen Nebel möglich (kostet Leistung).

## Partikel
- **Glühwürmchen** (`scripts/World/firefly_swarm.gd` / `World/firefly_swarm.tscn`): wenige **echte Lichter** (`OmniLight3D`), in der Welt verteilt, driften/blinken, blenden am Rand weich ein/aus, **nur nachts**.
- **Wald-Staub** (`scripts/World/forest_dust.gd` / `World/forest_dust.tscn`): kleine grüne Partikel **fest bei Sträuchern** (Höhenbereich `place_min/max_height`), nur im Umkreis (`radius`) sichtbar, weiches Distanz-Fade. Ziehen nicht mit dem Spieler mit.
- (Alte GPU-Variante `World/forest_particles.tscn` existiert noch, wurde durch `forest_dust` ersetzt.)

---

## Offene Punkte / TODO
- **Leistung**: GPU-Last hoch (~86 % auf 2080 Ti). Für Laptop-Ziel prüfen: volumetrischer Nebel aus, Anzahl Glühwürmchen-Lichter, Gras-Dichte/Radius, Schatten-/Baumdistanz. Später ein **Grafik-Menü** bauen, das diese zusammen regelt.
- `_zum_loeschen/` und ggf. `demo/`, alte `Models/terrain_3d.tscn`, `World/forest_particles.tscn` löschen.
- Blatt-Gegenlicht ggf. weiter dämpfen (Doppelseitigkeit), falls noch Aufleuchten.
- Automatische Tag-Nacht-Zeit (läuft von selbst) optional.
