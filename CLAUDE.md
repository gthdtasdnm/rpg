# CLAUDE.md – Schnelleinstieg für KI-Assistenten

Godot-Projekt (RPG). Diese Datei zuerst lesen, dann bei Bedarf `doc/roadmap.md` (was noch zu tun
ist) und `doc/fortschritt-2026-07-25.md` (Detailstand der Weltsysteme). Ziel: schnell orientiert,
keine Inkonsistenzen, wenig Token verbrannt.

## Engine / Stack
- **Godot 4.7**, **.NET/C#** aktiviert, **Forward+**.
- Addons: **Terrain3D** (GDExtension, macOS/Win/Linux-Binaries dabei), limboai, dialogue_manager.
- Sprache mit dem User: **Deutsch**.

## 🔧 Sprachregel: C# oder GDScript?
> **Spiel-Logik = C#.** GDScript nur für zwei Ausnahmen: **(1)** laufende, shader-nahe
> Weltsysteme, die schon fertig sind (`player.gd`, `day_night.gd`, `tree_collision.gd`,
> Partikel), **(2)** Addon-Kleber, der zwingend GDScript sein muss.

Grund: `dotnet build` ist ein Verifikationsschritt, den ein Agent selbst ausführen kann —
Tippfehler und falsche Signaturen fallen sofort auf statt erst im laufenden Spiel.
**Nach jeder C#-Änderung `dotnet build` laufen lassen** (muss 0 Fehler zeigen; die ~23 Warnungen
kommen alle aus `addons/` und sind normal).

Der Player bleibt vorerst GDScript (fertig, eng mit Gras/Wasser/Kamera verzahnt). Neue Logik
hängt sich als C#-Kind-Node an. Neubewertung beim Kampfsystem (M2), nicht vorher.

## ⚠️ Wichtigste Sache zuerst: es gibt ZWEI Ebenen, sie sind noch nicht verbunden
1. **Welt-Ebene (GDScript), läuft**: `World/`-Sandbox — Terrain, Himmel, Wasser, Gras, Bäume,
   Partikel, und der **GDScript-Player**.
2. **Spiel-Ebene (C#), fertig aber abgekoppelt**: `scripts/` (Characters, Combat, Dialogue,
   Quests, Magic, Items, …) + `Data/` (JSON) + `UI/Hud.tscn`.
   **Nicht „tot" oder wegwerfbar** — die Autoloads sind in `project.godot` **weiterhin
   registriert** (`GameFlags`, `GameData`, `DialogueRunner`, `QuestManager`, `SaveSystem`), das
   HUD (Leben, Dialogbox, Questlog `L`, Inventar `I`, Pause `Esc`, Speichern/Laden) existiert, und
   `Data/` enthält bereits **30 NPCs, 27 Dialoge, 47 Items, 7 Quests, 4 Zauber** passend zum
   Konzept.
   Was fehlt, sind nur die **Szenen dazwischen**: `Characters/Npc.tscn` (gelöscht), das HUD in
   `Main.tscn`, und `CharacterStats`/`Inventory`/`Interactor` am GDScript-Player.
   → **Das Wiederanschließen ist Verkabelung, keine Neuentwicklung** — laufender Sprint,
   siehe `doc/sprint-2026-08-02.md`. **Nie vorschlagen, das C#-Framework in GDScript neu zu
   schreiben.**
   → **`scripts/Characters/Player.cs` ist NICHT der aktive Player.** Aktiv ist
   **`scripts/World/player.gd`** (GDScript). Player bleibt GDScript, der Rest bleibt C#.
   → `doc/architektur.md` beschreibt noch die **gelöschte** Szenenstruktur (JSON-Schemas und
   Konventionen darin stimmen aber weiterhin). `doc/konzept/Story/Haupthandlung.md` +
   `Quests/Hauptquestreihe.md` sind **inhaltlich veraltet** — verbindlich ist die Kapitel-Struktur
   in `doc/roadmap.md` (Abschnitt 2).

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

## Spiel-Ebene (C#) – wo was liegt
- **Autoloads** (`project.godot`, Reihenfolge zählt): `GameFlags` (globale Flags, Konvention
  `talked_<Id>`, `quest_started_<Id>`, `quest_completed_<Id>`, `entered_<x>`, `learned_<Id>`),
  `GameData` (lädt `Data/**.json`), `DialogueManager` (Addon), **`Dialog`**
  (`scripts/Dialogue/DialogueBridge.cs`), `QuestManager`, `SaveSystem` (`user://savegame.json`).
- **Inhalte = Daten, kein Code**: neuer NPC/Item/Quest → JSON unter `Data/` (siehe
  `Data/README.md`); neuer Dialog → `.dialogue` unter `Dialogues/` (siehe `Dialogues/README.md`).
  Dateien mit `_` am Anfang werden von `GameData` ignoriert (Vorlagen/Entwürfe).
- **Dialoge laufen über das dialogue_manager-Addon**, nicht mehr über eigenen Code. Einziger
  Einstieg: `Dialog.Show(datei, titel, sprecher)`. Was Dialoge im Spiel auslösen dürfen, steht
  als öffentliche Methode in `DialogueBridge.cs` (`SetFlag`, `HasFlag`, `StartQuest`,
  `CompleteQuest`, `OpenShop`) — neue Möglichkeit = dort eine neue Methode.
  Dialogbox: `UI/DialogueBalloon.tscn` + `scripts/UI/DialogueBalloon.cs` (Kopien des
  Addon-Beispiels, damit Addon-Updates sie nicht überschreiben; eingestellt über
  `runtime/balloon_path`).
- **NPC**: `World/npc.tscn` instanzieren, im Inspector `CharacterId` + `DialogueFile` setzen.
  `SnapToGround` zieht ihn beim Start auf die Terrainhöhe — Position grob setzen reicht.
  `GreetOnApproach` = NPC hält den Spieler von selbst an (Gothic-Stil, `GreetArea` im NPC,
  Flag `greeted_<id>`), `TurnToPlayer` = dreht sich beim Ansprechen weich zum Spieler.
  Für echte Wegsperren: `Objects/GuardCheckpoint.tscn` (Barriere verschwindet, sobald das
  hinterlegte Flag gesetzt ist). `ProximityDialogue.cs` wurde dafür gelöscht — machte dasselbe
  schlechter.
- **Währung ist Silber** (`Inventory.Silver`, `price` in den Item-JSONs). Fähigkeiten sind Items
  vom Typ `skill`; gelernt wird als Flag `learned_<id>`, nicht ins Inventar.
- **Interaktion**: `Interactor` (RayCast3D an der Kamera, Taste `E`) + `IInteractable`
  (`Npc`, `ItemPickup`). ⚠️ Bekannte Falle: die Kamera sitzt **4,5 m hinter** dem Spieler, deshalb
  ist `target_position.z = -7.5` (≈3 m echte Reichweite). Der Spieler ist als Ausnahme
  eingetragen, sonst trifft der Strahl ihn selbst.
- **HUD**: `UI/Hud.tscn` + `scripts/UI/Hud.cs` — Lebensbalken, Silber, Interaktions-Hinweis,
  Inventar (`I`), Questlog (`L`), Pause/Speichern/Laden (`Esc`), Händlerfenster. Läuft mit
  `process_mode = Always`, alle Panels nutzen `GetTree().Paused`.
  Hängt in `Main.tscn` und erwartet am Spieler die Kind-Knoten **`Stats`** (`CharacterStats`,
  `CharacterId = "player"`), **`Inventory`**, **`Equipment`** sowie
  `CameraPivot/Camera3D/Interactor` — fehlt einer davon, stürzt `Hud._Ready()` ab.
- **Escape gehört dem HUD.** `player.gd` fasst den Mausmodus nicht mehr an; die Kamera dreht sich
  nur, solange `Input.mouse_mode == CAPTURED` ist.
- **`SaveSystem` ist sprachneutral**: sucht den Spieler als `Node3D` in der Gruppe `player` und
  ruft beim Laden `apply_save_state(position, rotation_y)` auf, falls vorhanden (in `player.gd`
  definiert — zieht den Kamera-Yaw nach, sonst springt die Kamera).
- **Noch nicht implementiert**: Kampf/Animationen, Trainer, Ausrüstungswirkung,
  Zonenwechsel/Teleport. Siehe `doc/roadmap.md`.

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

## Ordnerstruktur (Stand 26.07.)
`Assets/` = alles Sichtbare (Modelle + Texturen, **nach Domäne** sortiert, nicht nach Dateityp):
`Nature/Forest|Rocks`, `Terrain/Packed|Source`, `Buildings/`, `Props/`, `Weapons/`, `Characters/`.
Details, Herkunft und Lizenzen: **`Assets/README.md`**.
- **`Models/`, `Textures/`, `PackedTextures/` gibt es nicht mehr** — alles unter `Assets/`.
  Alte Doku, die diese Pfade nennt, ist veraltet.
- Die **gekauften Pakete sind nicht im Git-Repo** (`.gitignore`, ~800 MB); `Assets/Nature` und
  `Assets/Terrain` schon, weil bearbeitet/nicht reproduzierbar.
- ⚠️ **Assets verschieben**: nur im **Godot-Editor** (zieht Verweise automatisch nach). Per
  Terminal nur, wenn die `.import`-Datei mitwandert (hält die UID) **und** alle Pfade in
  `.tscn`/`.tres` ersetzt werden — danach prüfen, dass jeder `res://`-Pfad existiert.

## Aufräum-Reste (dürfen weg)
`_zum_loeschen/`, `demo/` (falls noch da), `World/forest_particles.tscn` (ersetzt).

## ⚠️ Terrain3D-Fallstricke (haben schon Stunden gekostet)
- **`World/data/assets.tres` wird NICHT gelesen.** Die Asset-Liste liegt eingebettet in
  `World/world.tscn` (`assets = SubResource(...)`). Änderungen an `assets.tres` bleiben wirkungslos.
- **Godot lädt extern geänderte Scripts nicht neu**, solange sie im Script-Editor offen sind —
  beim Ausführen läuft dann die alte Fassung. Tab schließen und neu öffnen.
- **Godot schreibt beim Speichern Werte in `project.godot` zurück** (gelöschte Autoloads tauchen
  wieder auf). Nach Datei-Edits kontrollieren.
- **Terrain3D verwirft die Position von LOD-Knoten** → Höhenversatz gehört über
  `PrimitiveMesh.center_offset` ins Mesh.
- **Baum-LODs**: Terrain3D sucht MeshInstance3D-Kinder namens `LOD0`, `LOD1`, … (nicht Godots
  eingebaute Mesh-LODs). Erzeugt von `scripts/Tools/generate_tree_imposters.gd`.

## 🔴 Offenes Problem
`doc/offen-blattschatten.md` — Blattschatten reichen nur ~20 m statt ~100 m, Stammschatten dagegen
weiter. Mehrere Lösungsversuche dokumentiert, alle erfolglos. **Vor eigenen Versuchen dort lesen**,
damit nicht dasselbe nochmal probiert wird.

## Doku-Wegweiser
- `doc/roadmap.md` – **Gesamtplan**: Meilensteine M1–M8, Story-Kapitelstruktur (verbindlich),
  Mengengerüst der Inhalte. Erste Anlaufstelle bei „was als Nächstes?".
- `doc/sprint-2026-08-02.md` – **aktueller Sprint** (M1: Framework anschließen) mit Abnahmetest.
- `doc/fortschritt-2026-07-25.md` – Detailbeschreibung aller Weltsysteme + Stellschrauben.
- `doc/backlog/` – was in vergangenen Sitzungen gebaut wurde (inkl. bereits gelöster Bugs —
  vor dem Debuggen kurz reinschauen).
- `doc/konzept/` – Spieldesign (Welt, Fraktionen, Items, Gameplay). Gut ausgearbeitet und gültig,
  **außer** Story/Haupthandlung + Quests/Hauptquestreihe (siehe Warnung oben).
