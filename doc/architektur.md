# Architektur-Konventionen

Leitplanke, damit neue Inhalte **additiv** hinzugefügt werden können, ohne bestehenden Code
anzufassen. Bei Unsicherheit: hier nachschauen, bevor eine neue Konvention erfunden wird.

## Ordnerschema

- `Data/<Kategorie>/*.json` — Inhalte (Charaktere, Items, Quests, Dialoge). Neue Instanz = neue
  JSON-Datei, kein Code nötig. `GameData` scannt die Ordner beim Start automatisch.
- `scripts/<Kategorie>/` — C#-Code, eine Kategorie pro Ordner (Data, Characters, Interaction,
  Items, Dialogue, Quests, UI). Namespace = `RPG.<Kategorie>`.
- `Characters/`, `Objects/`, `Maps/`, `UI/`, `Level/` — Godot-Szenen (`.tscn`), analog zu den
  Script-Kategorien.

## Neue Inhalte hinzufügen (Beispiele)

- **Neuer Charakter/NPC**: neue Datei `Data/Characters/npc_x.json`. Für einen sichtbaren NPC in
  der Welt: `Characters/Npc.tscn` in ein Level ziehen, `CharacterId` (auf `Npc`- und
  `Stats`-Node) setzen.
- **Neues Item**: neue Datei `Data/Items/x.json`. Für eine aufsammelbare Instanz:
  `Objects/Items/ItemPickup.tscn` platzieren, `ItemId` setzen.
- **Neue Quest**: neue Datei `Data/Quests/x.json`. `QuestManager` versteht aktuell nur
  `objectives[].type = "collect_item"`; weitere Typen brauchen einen neuen Fall in
  `QuestManager.NotifyItemCollected` (bzw. eine neue `NotifyXyz`-Methode für andere Auslöser).
- **Neuer Dialog**: neue Datei `Data/Dialogues/x.json`, per `dialogueId` an einem Charakter
  referenziert. Eine `choice.startQuest` löst automatisch `QuestManager.StartQuest` aus.

## JSON-Schemas

Siehe `scripts/Data/*.cs` für die genauen Felder (POCOs, per `System.Text.Json`,
case-insensitive deserialisiert). Kurzfassung:

- **Character**: `id, name, maxHealth, strength, dexterity, dialogueId?`
- **Item**: `id, name, type (weapon|consumable|material|quest), scalingStat? (strength|dexterity), damage, stackable`
- **Quest**: `id, title, giverNpcId?, objectives[{type, itemId, amount}], rewardItemIds[]`
- **Dialogue**: `id, startNode, startRules?[{requiresFlag?, requiresNotFlag?, node}],
  nodes{ [id]: { text, choices[{text, next?, startQuest?, completeQuest?, setFlag?, requiresFlag?, requiresNotFlag?}] } }`

## Flag-System (wie Gothics Weltvariablen)

`GameFlags` (Autoload, `scripts/World/GameFlags.cs`) ist ein globaler String-Speicher für
"ist passiert"-Zustände: `SetFlag(id)` / `HasFlag(id)`, Signal `FlagChanged`. Andere Systeme
kennen sich dadurch nicht direkt — sie setzen/lesen nur Flags.

Automatisch gesetzte Flags (Konvention, damit Namen vorhersagbar sind):
- `talked_<CharacterId>` — von `Npc.Interact()`, sobald der NPC das erste Mal angesprochen wird.
- `quest_started_<QuestId>` — Quest wurde gestartet.
- `quest_ready_<QuestId>` — alle Objectives erfüllt, aber noch nicht abgegeben (siehe Quest-Abgabe
  unten).
- `quest_completed_<QuestId>` — Quest wurde beim NPC abgegeben.
- `entered_<x>` — von `LocationTrigger` (`Objects/Triggers/LocationTrigger.tscn`, `FlagId` frei
  wählbar), wenn der Spieler eine Area3D betritt.

Dialoge können Flags abfragen:
- `DialogueDefinition.startRules` wählt beim Start den Einstiegsknoten anhand von Flags (erste
  zutreffende Regel gewinnt, sonst `startNode`) — z.B. andere Begrüßung, wenn eine Quest schon
  läuft/fertig ist.
- `DialogueChoice.requiresFlag` / `requiresNotFlag` blendet einzelne Antwortmöglichkeiten ein/aus.
- `DialogueChoice.setFlag` setzt beim Wählen ein beliebiges Flag (zusätzlich zu `startQuest`).
- `DialogueChoice.completeQuest` schließt eine Quest ab (siehe Quest-Abgabe unten) — wie
  `startQuest`, aber fürs Beenden.

### Quest-Abgabe (kein Auto-Complete)

Eine Quest schließt **nicht** automatisch ab, sobald alle Objectives erfüllt sind — `QuestManager`
setzt dann nur `quest_ready_<id>` und bleibt in `_active`. Erst eine Dialog-Choice mit
`completeQuest` (z.B. "Hier hast du deine Kräuter.") schließt sie wirklich ab: verbrauchte
`collect_item`-Objectives werden aus dem Inventar entfernt, Belohnungs-Items vergeben,
`quest_completed_<id>` gesetzt. Ein NPC-Dialog braucht dafür i.d.R. 3 `startRules`-Stufen in
dieser Reihenfolge (erste passende gewinnt): `quest_completed_<id>` (Dank-Text) →
`quest_ready_<id>` (Abgabe-Knoten mit `completeQuest`-Choice) → `quest_started_<id>`
(Zwischenstand-Text) → sonst `startNode` (Erstkontakt). Siehe `Data/Dialogues/gerald_intro.json`
als Referenz.

Neues Flag-gesteuertes Verhalten hinzufügen = neues Flag in einer JSON-Datei referenzieren, kein
Code nötig — es sei denn, das Flag soll automatisch durch ein *neues* Ereignis gesetzt werden
(dann an der jeweiligen Stelle, z.B. einem neuen Interactable, `GameFlags.Instance.SetFlag(...)`
ergänzen).

## Stats & Magie (Regeln, aktuell nur als Datenmodell vorbereitet)

- Jeder Charakter hat `Health` (Leben), `Strength` (Stärke) und `Dexterity` (Geschicklichkeit).
- Waffen skalieren über `Item.ScalingStat`: Schwerter mit `strength`, Bögen mit `dexterity`.
- Magie-System (noch nicht implementiert, da aktuell kein Kampf/keine Gegner existieren, an
  denen es sich zeigen würde):
  - Zwei Schulen: **göttlich** und **dämonisch**. Beide nutzen `CharacterStats.CurrentHealth`
    als gemeinsame Ressource statt eines separaten Mana-Werts.
  - **Dämonisch**: kostet Leben direkt beim Wirken, sonst kampftauglich (schnell, riskant).
  - **Göttlich**: lange Kanalzeit, Bewegung währenddessen gesperrt — dadurch nur für Effekte
    gedacht, die man *nicht* im Kampf braucht (z.B. Heilung/Buffs außerhalb von Gefechten).
  - Wenn Kampf/Gegner gebaut werden: `SpellCaster`-Komponente analog zu `CharacterStats` als
    neue Kategorie `scripts/Magic/` ergänzen, ohne bestehende Systeme zu ändern.

## HUD & Pause-Menü

`Hud` (`UI/Hud.tscn` + `scripts/UI/Hud.cs`) ist der zentrale UI-Koordinator, `process_mode =
Always`, damit es auch während `GetTree().Paused` auf Tasten reagiert. Ein einfacher
`PanelKind`-Zustand (`None/Pause/Inventory/QuestLog`) verhindert, dass mehrere Panels gleichzeitig
offen sind. `OpenPanel`/`ClosePanel` setzen zentral `GetTree().Paused` + Mausmodus — Player/NPCs
frieren dadurch automatisch (Default-`ProcessMode = Pausable`), ohne eigenen Freeze-Code. Das
Dialog-System bleibt davon getrennt (eigener `DialogueRunner.IsActive`-Check in `Player.cs`);
Pause/Inventar/Questlog lassen sich absichtlich nicht während eines Dialogs öffnen.

- **Escape**: schließt ein offenes Panel, sonst öffnet es das Pause-Menü.
- **`I`** (`toggle_inventory`) / **`L`** (`toggle_questlog`): öffnen/schließen Inventar bzw.
  Questlog (nur wenn kein anderes Panel offen ist).
- Lebensbalken ist immer sichtbar, keine Taste nötig.
- Neues Panel hinzufügen = gleiches Muster: eigener `PanelKind`-Wert, in `OpenPanel`/`ClosePanel`
  Sichtbarkeit ergänzen, Refresh-Methode beim Öffnen aufrufen.

## Speichersystem

`SaveSystem` (Autoload, `scripts/World/SaveSystem.cs`) schreibt/liest **ein** Savegame nach
`user://savegame.json` (JSON via `System.Text.Json`, gleiches Muster wie `GameData`, nur
schreibend). Enthält: Spielerposition/-drehung, Leben, komplettes Inventar, alle `GameFlags`,
Fortschritt aktiver Quests (`SaveData`, `scripts/World/SaveData.cs`).

Abgeschlossene Quests brauchen keinen eigenen Save-Eintrag — `quest_completed_<id>` ist schon Teil
der gespeicherten Flags, das Questlog leitet seinen Inhalt komplett aus `GameData.GetAllQuests()`
+ `GameFlags` + laufendem `QuestManager`-Fortschritt ab (`QuestManager.GetQuestLogEntries()`),
keine doppelte Buchführung.

Neue speicherbare Daten hinzufügen = Feld in `SaveData` ergänzen + in `SaveSystem.Save()`/`Load()`
befüllen/anwenden. Beim Laden über `Player.ApplySaveState(position, yaw)` gehen (nicht Position
direkt setzen), sonst desynct der intern getrackte Kamera-Yaw und die Kamera "springt".

## Bewusste Vereinfachungen (v1)

- Nur ein Savegame-Slot (kein Save-Auswahlmenü, jedes Speichern überschreibt).
