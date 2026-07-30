# Technik: Weltsysteme, Stellschrauben, Fallstricke

Wie die laufenden Systeme funktionieren und wo man dreht. Kurzfassung in `CLAUDE.md`.

---

## 1. Szenenaufbau

```
Main.tscn                       Hauptszene
├── World  (world.tscn)         die ganze Welt, eine Datei
├── Player (player.tscn)        GDScript-Player, Gruppe "player"
├── Hud    (UI/Hud.tscn)        Leben, Silber, I, L, Esc, Händler
├── PerformanceOverlay
└── Halvor / Falk / Knud        NPCs (npc.tscn, CharacterId + DialogueFile)
```

⚠️ **`world.tscn` enthält einen zweiten Player** (Stand 30.07.). Da `Main.tscn` beide lädt, laufen
zwei Spieler gleichzeitig: zwei Kameras, zwei Körper, beide in Gruppe `player`. `SaveSystem` und
`tree_collision.gd` nehmen den, der zuerst gefunden wird. **Sollte aus `world.tscn` gelöscht
werden.**

### Aufbau von `world.tscn`

Seit 30.07. ist alles in einer Datei — `bewuchs.tscn`, `orte.tscn` und `flaechen.tscn` sind
aufgelöst (vorher instanziert, jetzt direkte Kinder). 3265 Nodes, 754 KB.

```
World/
├── Terrain3D/          Terrain3DParticles (Gras) — MUSS hier bleiben, NodePath("..")
├── Systeme/            Environment, TreeCollision, ShadowTuner, FireflySwarm, ForestDust
├── wasser              MeshInstance3D + wasser.gd
├── Player              ⚠️ Duplikat, siehe oben
├── Barriere
├── Blockout/           CSGCombiner3D — Nethora (50), umgebung (55), FocusSteine (5)
├── Bauten/             12 Gebäudeteile + Jägerhütte
├── Orte/               60 Blocking-Boxen in 10 Gruppen
└── Bewuchs/
    ├── rocks_umgebung/ 18 Felsen — Gruppe NICHT auflösen, siehe unten
    └── Sektor_x…_z…/   28 Sektoren à 200 m, darin Baeume/ Felsen/ Straeucher/ + benannte Flächen
```

**Warum Sektoren:** Bei 2400 Objekten flach untereinander findet man nichts. Die Sektornamen
tragen die Koordinaten — Position eines Baums im Inspector ablesen sagt direkt, welcher Sektor.
Pro Sektor das Auge-Icon klicken blendet den Bereich aus.

⚠️ **`Bewuchs/rocks_umgebung` darf nicht aufgelöst werden.** Der Knoten trägt
`scripts/World/rock_collision.gd`, das **allen Steinen unter diesem Knoten** beim Start Kollision
gibt. Die 18 Felsen darin sind direkte `.glb`-Instanzen ohne eigene Collider — ohne die Gruppe
haben sie keine Kollision.

**Keine Gruppe hat einen Transform.** Alle Positionen sind absolut, Umhängen ist also
positionsneutral.

---

## 2. Die laufenden Systeme

| System | Dateien |
|---|---|
| **Player** | `World/player.tscn` + `scripts/World/player.gd` — WASD kamerarelativ, Maus-3rd-Person, Shift=rennen, Springen, Schwimmen (hält an der Oberfläche, wippt, neigt sich), Kamera-Clamp über der Wasserlinie |
| **Tag/Nacht + Himmel** | `World/Environment.tscn`, `scripts/World/day_night.gd` (Regler `Time Of Day` 0–24, stellt `night_factor` bereit), `World/sky.gdshader` (Gradient, Sonne + Halo, Mond, Sterne, ziehende Wolken) |
| **Wasser** | `Objects/water.gdshader` + `Objects/wasser.gd` — tiefenabhängige Farbe, Refraktion, Reflexion (Fresnel), Kielwellen beim Schwimmen (nur Normalen, nicht Refraktion — sonst Artefakte) |
| **Gras** | `addons/terrain_3d/extras/particle_example/` — Spieler-Trail (Gras teilt sich, Spur bleibt kurz offen), Wind im Pfad ausgeblendet, Fake-AO am Halmgrund. Wächst nur auf bestimmten Textur-IDs |
| **Terrain-Boden** | `World/terrain_shader.gdshader` als `shader_override` — dunkelt den Boden ab, wo Gras wächst; teilt `grass_noise.tres` mit dem Gras |
| **Bäume** | Handplatziert als Nodes in `world.tscn`. Modelle in `Assets/Nature/Forest/merged/`, Kollision aus den `.tscn` selbst |
| **Baumkollision (Instancer)** | `scripts/World/tree_collision.gd` — Zylinder nur im Umkreis des Spielers, wandernder Pool. **Inzwischen wirkungslos**, da keine Instancer-Bäume mehr da sind. Kann weg |
| **Felskollision** | `scripts/World/rock_collision.gd` — TRIMESH/CONVEX/DECOMPOSED wählbar, `print_summary` misst Formen/Dreiecke/ms |
| **Partikel** | `scripts/World/firefly_swarm.gd` (Glühwürmchen = echte `OmniLight3D`, nur nachts) · `scripts/World/forest_dust.gd` (grüner Staub bei Sträuchern, distanzbasiert) |

---

## 3. Spiel-Ebene (C#)

**Autoloads** (`project.godot`, Reihenfolge zählt): `GameFlags` · `GameData` (lädt `Data/**.json`)
· `DialogueManager` (Addon) · **`Dialog`** (`scripts/Dialogue/DialogueBridge.cs`) · `QuestManager`
· `SaveSystem` (`user://savegame.json`)

**Flag-Konvention:** `talked_<Id>` · `quest_started_<Id>` · `quest_completed_<Id>` ·
`entered_<x>` · `learned_<Id>` · `greeted_<id>`

**Dialoge** laufen über das dialogue_manager-Addon. Einziger Einstieg: `Dialog.Show(datei, titel,
sprecher)`. Was Dialoge auslösen dürfen, steht als öffentliche Methode in `DialogueBridge.cs`
(`SetFlag`, `HasFlag`, `StartQuest`, `CompleteQuest`, `OpenShop`) — neue Möglichkeit heißt: dort
eine neue Methode. Dialogbox: `UI/DialogueBalloon.tscn` + `scripts/UI/DialogueBalloon.cs` (Kopien
des Addon-Beispiels, damit Updates sie nicht überschreiben; eingestellt über `runtime/balloon_path`).

**NPC:** `World/npc.tscn` instanzieren, im Inspector `CharacterId` + `DialogueFile` setzen.
`SnapToGround` zieht ihn auf die Terrainhöhe — Position grob setzen reicht. `GreetOnApproach` =
NPC hält den Spieler von selbst an (Gothic-Stil, `GreetArea`, Flag `greeted_<id>`).
`TurnToPlayer` = dreht sich weich zum Spieler. Für echte Wegsperren:
`Objects/GuardCheckpoint.tscn`.

**Interaktion:** `Interactor` (RayCast3D an der Kamera, Taste `E`) + `IInteractable` (`Npc`,
`ItemPickup`). ⚠️ Die Kamera sitzt **4,5 m hinter** dem Spieler, deshalb ist
`target_position.z = -7.5` (≈3 m echte Reichweite). Der Spieler ist als Ausnahme eingetragen,
sonst trifft der Strahl ihn selbst.

**HUD** (`UI/Hud.tscn` + `scripts/UI/Hud.cs`) erwartet am Spieler die Kinder **`Stats`**
(`CharacterStats`, `CharacterId = "player"`), **`Inventory`**, **`Equipment`** sowie
`CameraPivot/Camera3D/Interactor` — **fehlt einer, stürzt `Hud._Ready()` ab.** Läuft mit
`process_mode = Always`, alle Panels nutzen `GetTree().Paused`.

**Escape gehört dem HUD.** `player.gd` fasst den Mausmodus nicht an; die Kamera dreht sich nur,
solange `Input.mouse_mode == CAPTURED`.

**`SaveSystem` ist sprachneutral:** sucht den Spieler als `Node3D` in Gruppe `player` und ruft beim
Laden `apply_save_state(position, rotation_y)` auf (in `player.gd` definiert — zieht den Kamera-Yaw
nach, sonst springt die Kamera).

### Ist-Stand der Inhalte

| Ordner | Vorhanden |
|---|---|
| `Data/Characters/` | **3 NPCs** (Halvor, Falk, Knud) + `player.json` + Vorlage |
| `Data/Items/` | **4** |
| `Data/Quests/` | **1** (`quest_nach_nethora`) + Vorlage |
| `Data/Spells/` | **1** |
| `Dialogues/` | **3** |

⚠️ Frühere Doku sprach von „30 NPCs, 27 Dialoge, 47 Items, 7 Quests, 4 Zauber". **Das ist falsch**
— die JSONs wurden am 26.07. geleert und nie ersetzt. Inhalte müssen neu geschrieben werden, nach
`doc/welt.md` und `doc/Kapitelverlauf.txt`.

---

## 4. Terrain3D-Fallstricke

Alle haben schon Zeit gekostet.

- **`World/data/assets.tres` wird NICHT gelesen.** Die Asset-Liste liegt eingebettet in
  `world.tscn` (`assets = SubResource(...)`). Änderungen an `assets.tres` bleiben wirkungslos.
- **Nur ein Terrain3D-Node pro Szene.** Zwei Terrain3D auf dasselbe `data_directory` heißt: beide
  lesen und schreiben dieselben Regionsdateien, Bemalen landet ins Nichts oder wird überschrieben.
  Genau das war am 30.07. der Grund, warum sich das Terrain nicht bemalen ließ.
- **Instancer erzeugt nur Optik (MultiMesh), keine Kollision.**
- **Kein Getter für Instanz-Transforms** — man liest die `MultiMeshInstance3D` unter dem Node.
- **Terrain3D verwirft die Position von LOD-Knoten** → Höhenversatz gehört über
  `PrimitiveMesh.center_offset` ins Mesh.
- **Baum-LODs:** Terrain3D sucht `MeshInstance3D`-Kinder namens `LOD0`, `LOD1`, … — nicht Godots
  eingebaute Mesh-LODs. Erzeugt von `scripts/Tools/generate_tree_imposters.gd`.
- **Objekte nicht als Kinder von Terrain3D hängen** (der Node hat `top_level = true` und
  `_edit_lock_`). Ausnahme: `Terrain3DParticles`, das braucht `NodePath("..")`.

## 5. Godot-Fallstricke

- **Godot überschreibt `.tscn` beim Speichern.** Nach Datei-Edits von außen den Editor **neu laden**
  lassen. Bei großen `.tscn` vorsichtig editieren (vorher lesen). Shader und `.gd` sind unkritisch.
- **Godot lädt extern geänderte Scripts nicht neu**, solange sie im Script-Editor offen sind — beim
  Ausführen läuft die alte Fassung. Tab schließen und neu öffnen.
- **Godot schreibt beim Speichern Werte in `project.godot` zurück** (gelöschte Autoloads tauchen
  wieder auf). Nach Datei-Edits kontrollieren.
- **Editor-RAM ≠ Spiel-Performance.** Für Perf immer FPS/ms im **laufenden Spiel** messen.
- **Godot batcht statische Meshes nicht.** 2400 Baum-Nodes = mindestens 2400 Draw Calls. Die
  Gegenmaßnahme ist `visibility_range_end` an den Baumszenen (Inspector → *Visibility Range*) —
  aktuell **nicht gesetzt**, und bei ~86 % GPU-Last der nächste sinnvolle Schritt.

---

## 6. Gelöste Probleme (nicht nochmal versuchen)

### Blattschatten reichten nur ~20 m — GELÖST 27.07.

**Ursache:** `fade_margin > 0` lässt Terrain3D `visibility_range_fade_mode = SELF` setzen. In
Godot 4 zerstört das die Schatten von Materialien mit **Alpha Clip / Alpha Hash** — und genau das
sind die Blattmaterialien (`transparency = 2`) und der Imposter-Shader. Es war **nie** eine falsch
eingestellte Distanz.

- [godot#91671](https://github.com/godotengine/godot/issues/91671) — Visibility Range „Fade Self"
  hides shadows in Depth Pre-Pass
- [godot#88854](https://github.com/godotengine/godot/issues/88854) — Visibility Range Fade with
  Alpha Clip and Alpha Hash

**Workaround, der gilt: `fade_margin = 0` bei allen Baum-Assets. Nicht wieder hochsetzen.** A/B
nachgewiesen: `0` → durchgehender Schatten + hartes Popping, `6` → weicher Übergang +
Schattenlücke. Bei diesem Engine-Fehler gibt es kein Beides. Ein weicher LOD-Übergang ohne Godots
kaputte Fade-Funktion bliebe der einzige verbleibende Weg.

**Vergeblich versucht** (nicht wiederholen): `alpha_scissor_threshold` senken (0.5→0.2) ·
`directional_shadow_max_distance` erhöhen (wurde schlechter) und senken · Schattenkarte 4096→8192
· MSAA + Alpha-to-Coverage (weiße Kreise) · `distance_fade_mode = 3` (Blätter verblassten beim
Schwenk) · Blob-Schatten unter den Impostern (funktionierte, kollidierte mit der LOD-Überblendung).

### Weitere gelöste Punkte

- **Baummodelle waren Stamm + Blätter als getrennte Meshes** → Terrain3D las sie als LODs. Fix:
  `scripts/Tools/merge_tree_meshes.gd` führt sie zu Einzel-Meshes zusammen (`merged/`).
- **Weißes Gegenlicht-Aufleuchten an Bäumen** → `specular_mode` auf allen Baum-Materialien aus.
- **Doppeltes Terrain in `bewuchs.tscn`** → gelöscht (30.07.). Das war auch der Grund für 13 MB
  Dateigröße: eingebettete Texturen als Base64-Text.

---

## 7. Leistung

GPU-Last hoch, **~86 % auf einer RTX 2080 Ti** — Ziel sind auch Laptops. Teuerste Posten in
Reihenfolge:

1. **Gras** (kostet ~8× so viel wie die Bäume)
2. **Volumetrischer Nebel** (God Rays); Nebel läuft sonst als Depth-Fog
3. **Anzahl OmniLights** (Glühwürmchen)
4. **Wasser-Shader**
5. **Schatten- und Baumdistanz**, `lod0_range`
6. **Draw Calls** durch die 2400 Baum-Nodes → `visibility_range_end` setzen

Später ein **Grafik-Menü**, das diese Werte zusammen regelt.

---

## 8. Offene technische Punkte

- **Zweiter Player in `world.tscn`** löschen (siehe Abschnitt 1)
- **`visibility_range_end`** an den Baumszenen setzen
- **Eingebettete Meshes:** `Objects/rocks/` (18 MB) und `Objects/trees/` (5 MB) enthalten die
  Geometrie als **Textarrays in den `.tscn`**, obwohl die `.glb`-Quellen in `Assets/Nature/`
  liegen. Godot muss das als Text parsen — der größte verbleibende Ladezeit-Posten. Fix: `.glb`
  verlinken statt einkopieren, Kollision über *Advanced Import Settings* → „Physics Body
  generieren"
- **`TreeCollision`** ist wirkungslos und kann weg
- **`ItemPickup`** in der Terrain-Welt noch nicht getestet
- **„Benutzen"-Knopf** für Verbrauchsgüter im Inventar fehlt
- **Maßstabsfrage:** `player.gd` hat `speed = 12.0` (720 m/min), das Tal ist auf Spielerhöhe ~180 m
  breit — Gothics Schluchten liegen bei 40–60 m. Entweder Tempo runter oder Enge erhöhen. Wirkt
  sich auf jede spätere Platzierung aus
- **Die Eröffnungsansicht ist nicht gestaltet.** Regeln erzeugen Grundlast und Rhythmus, aber die
  Rahmung des ersten Blicks ist Handarbeit aus einer Kameraposition auf Augenhöhe
- Automatische Tag-Nacht-Zeit (läuft von selbst) optional

---

## 9. Ordnerstruktur

`Assets/` = alles Sichtbare, **nach Domäne** sortiert: `Nature/Forest|Rocks` ·
`Terrain/Packed|Source` · `Buildings/` · `Props/` · `Weapons/` · `Characters/`. Herkunft und
Lizenzen in `Assets/README.md`.

- **`Models/`, `Textures/`, `PackedTextures/` gibt es nicht mehr.**
- Die **gekauften Pakete sind nicht im Git-Repo** (`.gitignore`, ~800 MB). `Assets/Nature` und
  `Assets/Terrain` schon, weil bearbeitet und nicht reproduzierbar.
- ⚠️ **Assets verschieben nur im Godot-Editor** (zieht Verweise nach). Per Terminal nur, wenn die
  `.import`-Datei mitwandert (hält die UID) **und** alle Pfade in `.tscn`/`.tres` ersetzt werden.

---

## 10. Die Bewuchs-Generierungsskripte

`scripts/Tools/place_vegetation.py` und `place_orte.py` haben den ursprünglichen Bewuchs
regelbasiert aus den Terrain3D-Höhendaten erzeugt (Felsen am Fuß von Steilhängen, Baumgruppen mit
gaußförmiger Streuung, Artenwahl nach Höhenlage, Straßenrahmung abwechselnd links und rechts,
Freihaltezonen für Straße, Flussbett, Nethora und Startkorridor).

🔴 **Nicht mehr ausführen.** Sie schreiben `World/bewuchs.tscn` und `World/orte.tscn` — beide
Dateien existieren nicht mehr, ihr Inhalt liegt seit 30.07. handbearbeitet in `world.tscn`. Ein
Lauf würde verwaiste Dateien erzeugen und, falls wieder eingebunden, die gesamte Handarbeit
überschreiben.

Nützlich bleiben die Leseskripte, falls man das Terrain nochmal von außen auswerten will:
`terrain3d_read.py` (entpackt die ZSTD-blockkomprimierten `.res`) und `terrain3d_maps.py` (Höhe,
Control, Farbe als NumPy-Arrays). Jede Region enthält drei 1024×1024-Bilder: Höhe (`RFloat`),
Control (`uint32`), Farbe (`RGBA8`). Textur 2 = Straßen, Textur 3 = Flussbett.
