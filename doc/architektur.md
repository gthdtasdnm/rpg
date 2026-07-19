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
- **Neue Quest**: neue Datei `Data/Quests/x.json`. `QuestManager` versteht `objectives[].type =
  "collect_item"` (nutzt `itemId`/`amount`), `"talk_to_npc"` (nutzt `targetId` = CharacterId,
  ausgelöst von `Npc.Interact()`), `"reach_location"` (nutzt `targetId` = FlagId, ausgelöst von
  `LocationTrigger`) und `"kill_enemy"` (nutzt `targetId` = CharacterId des Gegners, `amount`,
  ausgelöst von `Enemy.OnDied`). Weitere Typen: neuer Fall + eigene `NotifyXyz`-Methode nach
  demselben Muster. `rewardGold` zusätzlich zu `rewardItemIds` möglich.
- **Neuer Dialog**: neue Datei `Data/Dialogues/x.json`, per `dialogueId` an einem Charakter
  referenziert. Eine `choice.startQuest` löst automatisch `QuestManager.StartQuest` aus,
  `choice.openShop` öffnet das Shop-Panel des sprechenden NPCs (siehe "Handel" unten).
- **Neues Gebiet**: neue Datei `Level/Zones/x.tscn` (Root-Node-Typ **muss** `Node3D` sein, kein
  eigenes Environment/Ground), als Kindnode mit eigenem Transform-Offset in `Level/World.tscn`
  instanzieren (siehe "Welt" unten) — kein Zonenwechsel-Code mehr, alles ist durchgehend begehbar.
- **Neuer Händler**: `CharacterDefinition.ShopItemIds` befüllen, im Dialog eine Choice mit
  `openShop: true` ergänzen (siehe "Handel" unten).
- **Neuer Waffentrainer**: wie Händler, aber `ShopItemIds` enthält ein `skill_*`-Item
  (siehe "Handel"/"Training" unten) statt echter Waren.
- **NPC, der den Spieler proaktiv anspricht**: `Objects/Triggers/ProximityDialogue.tscn` neben dem
  NPC platzieren (siehe "Proaktive NPC-Interaktion" unten).
- **Zugangskontrolle/Wache, die den Weg versperrt**: `Objects/Triggers/GuardCheckpoint.tscn`
  platzieren (siehe "Proaktive NPC-Interaktion" unten).

## JSON-Schemas

Siehe `scripts/Data/*.cs` für die genauen Felder (POCOs, per `System.Text.Json`,
case-insensitive deserialisiert). Kurzfassung:

- **Character**: `id, name, maxHealth, strength, dexterity, dialogueId?, shopItemIds[]`
  (+ Gegner-Felder, siehe `CharacterDefinition.cs`)
- **Item**: `id, name, type (weapon|armor|shield|consumable|material|quest|skill),
  scalingStat? (strength|dexterity), damage, damageType? (slashing|blunt|mixed|ranged),
  weaponCategory? (onehand|twohand|bow), defense, staggerResist, healAmount, price, stackable`
- **Quest**: `id, title, giverNpcId?, objectives[{type, itemId, targetId?, amount}],
  rewardItemIds[], rewardGold`
- **Dialogue**: `id, startNode, startRules?[{requiresFlag?, requiresNotFlag?, node}],
  nodes{ [id]: { text, choices[{text, next?, startQuest?, completeQuest?, setFlag?, requiresFlag?,
  requiresNotFlag?, openShop?}] } }`

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

## Welt (`Level/`)

**Geschichte/Entscheidung**: Ursprünglich (Phase 2, erster Anlauf) gab es ein Zonen-Wechsel-
System (`ZoneManager`/`ZoneTransition`), das den Karteninhalt zur Laufzeit austauschte, analog zu
Gothic 1's Gebietsladen. Der Nutzer wollte stattdessen ausdrücklich eine **durchgehende offene
Welt** ("alles schon geladen, selbst hinlaufen, alles sehen können") — das Zonen-Wechsel-System
wurde daraufhin komplett entfernt (`ZoneManager.cs`, `ZoneTransition.cs`,
`Objects/Triggers/ZoneTransition.tscn`, `SaveData.CurrentZoneScenePath` — alles gelöscht, nicht
nur deaktiviert).

Jetzt: `Level/World.tscn` ist die `run/main_scene` und enthält **alles gleichzeitig**:
`Player`, `Hud`, ein gemeinsames `Environment`+`Ground` (genau einmal — die einzelnen
Gebiets-Dateien haben **kein** eigenes Environment/Ground mehr, sonst gäbe es mehrere
überlappende `WorldEnvironment`/Kollisionsebenen) und vier Gebiets-Szenen als Kindnodes mit
Transform-Offset, sodass sie sich räumlich nicht überlappen:

| Gebiet | Datei | Offset in `World.tscn` |
|---|---|---|
| Ankunft | `Level/Zones/Ankunft.tscn` | `(0, 0, 0)` |
| Nethora | `Level/Zones/Nethora.tscn` | `(0, 0, -70)` |
| Freies Lager | `Level/Zones/FreiesLager.tscn` | `(-90, 0, -80)` |
| Sektentempel | `Level/Zones/Sektentempel.tscn` | `(90, 0, -80)` |

Der Ordner heißt weiterhin `Level/Zones/`, auch wenn es keine Zonen-*Wechsel*-Logik mehr gibt —
die einzelnen Dateien sind einfach räumlich getrennte Gebietsinhalte, die alle zusammen in einer
Szene existieren. Root-Node-Typ jeder Gebiets-Datei muss `Node3D` sein (sonst brechen globale
Transforms der Kinder unter dem Offset-Node in `World.tscn`).

**Neues Gebiet hinzufügen**: neue Datei `Level/Zones/x.tscn` (Node3D-Root, ohne eigenes
Environment/Ground), als Kindnode mit passendem Transform-Offset in `World.tscn` instanzieren —
Offset so wählen, dass sich die Bounding-Box nicht mit bestehenden Gebieten überschneidet
(Abstand von mindestens der Summe der halben Ausdehnungen beider Gebiete plus Puffer für einen
begehbaren Übergangsweg).

`GuardCheckpoint`/`ProximityDialogue` (siehe "Proaktive NPC-Interaktion" unten) sind von dieser
Umstellung unberührt — sie hingen nie von `ZoneManager` ab, nur `GameFlags`/`DialogueRunner`.

## Kampf (`scripts/Combat/`)

- `IDamageable` (Schaden nehmen, `TakeDamage(amount, damageType?)`) implementieren `Player` und
  `Enemy`. `DamageType` folgt `doc/konzept/Gameplay/Kampfsystem.md` (slashing/blunt/mixed/ranged).
- `Enemy`: eigener Gegner-Node (CharacterBody3D), nutzt aber dieselbe Datenquelle wie NPCs
  (`CharacterDefinition`/`CharacterStats` aus `Data/Characters/*.json`) — Gegner sind einfach
  Einträge mit befüllten `AttackDamage`/`ResistantTo`/`WeakTo`/`AggroRadius`/... Feldern statt
  `DialogueId`. Einfache KI: verfolgt Ziele in Gruppe `"player"` innerhalb `AggroRadius`, greift
  in `AttackRange` direkt an (kein eigenes Hitbox-Timing). Loot fällt beim Tod als `ItemPickup`
  ab (`LootItemIds`).
- `Equipment`: manuelles Anlegen über `Equip(itemId)` — sortiert nach `ItemDefinition.Type` in
  den passenden Slot (`weapon`/`shield`/`armor`). Ausgelöst über den "Ausrüsten"-Knopf im
  Inventar-Panel (`Hud.RefreshInventory`), kein Auto-Equip mehr. Nicht ausrüstbare Typen
  (`material`/`consumable`/`quest` — darunter auch die Runen aus `doc/konzept/Items/Runen.md`,
  die als Zauber-Reagenz statt als Ausrüstung gedacht sind) tauchen im Inventar ohne Knopf auf.
- Spieler-Nahkampf: `MeleeHitbox` (Area3D-Kind von `Player`), Schaden wird beim Tastendruck sofort
  anhand der aktuellen Überlappung ausgewertet (kein Windup/Aktiv-Fenster, mangels
  Animationssystem). Schaden/Schadensart kommen von `Equipment.EquippedWeaponId`, sonst
  Notfall-Basiswerte (bloße Hand).
  - Zwei Tasten (`attack_left`/`attack_right`, Maus links/rechts) statt einer: aufeinanderfolgende
    Treffer müssen die Seite wechseln, um als "flüssig" zu gelten (`Player._lastAttackSide`,
    `_comboWindowTimer`) — nur dann gibt's kürzeren Folge-Cooldown und einen Combo-Schadensbonus
    (`_comboCount`, max. `MaxComboStacks`). Zweimal dieselbe Taste oder zu langsam reagiert
    zurückgesetzt die Combo auf 1 und der nächste Angriff braucht länger (`ChoppyAttackCooldown`).
    Grundlage für das spätere Timing-/Kombosystem aus `doc/konzept/Gameplay/Kampfsystem.md` —
    aktuell noch ohne echtes Timing-Fenster. Die Combo selbst ist an Waffentraining gekoppelt
    (`Player.IsComboUnlocked`, siehe "Training" unten) — ohne das passende `learned_*`-Flag gibt's
    nur Basisangriffe, unabhängig vom Klickmuster.
- Blocken (`block`-Taste, jetzt `B` statt Maus rechts — die ist für `attack_right` belegt)
  reduziert eingehenden Schaden stärker mit Schild als ohne (siehe `Player.TakeDamage`); starke
  Treffer bzw. Blocken ohne Schild lösen kurzen Stagger aus (`_staggerTimer`, sperrt Angriff/
  Block, keine Bewegungssperre). Rüstung (`Equipment.EquippedArmorId`) zieht vor der
  Block-Berechnung `ItemDefinition.Defense` flach vom eingehenden Schaden ab.

## Handel (`Hud.cs` ShopPanel)

- Währung: `Inventory.Gold` (einfaches `int`, kein Item-Stack). `AddGold`/`SpendGold`/
  `RestoreGold`, Signal `GoldChanged` — HUD zeigt es permanent (`GoldLabel`) und im Shop-Panel.
- Ein Händler = ein `CharacterDefinition.ShopItemIds`-Eintrag (1:1, keine eigene `Data/Shops/`-
  Kategorie nötig, solange kein Sortiment geteilt wird). `choice.OpenShop: true` in einem Dialog-
  Knoten löst `DialogueRunner.ShopRequested(characterId)` aus (braucht `speakerCharacterId` beim
  `DialogueRunner.Start(...)`-Aufruf, siehe `Npc.Interact()`), `Hud` öffnet darauf das
  `ShopPanel` (neuer `PanelKind.Shop`, gleiches Prinzip wie Inventar/Questlog).
- Kaufen: Preis (`ItemDefinition.Price`) wird von `Inventory.Gold` abgezogen, Item landet im
  Inventar. Verkaufen: Inventar-Items mit `Price > 0` und `Type != "quest"` können verkauft
  werden, Verkaufspreis = `Price / 2` (abgerundet, mindestens 1).
- `ItemDefinition.Type == "skill"` ist ein Sonderfall: kein Inventareintrag, stattdessen wird
  beim Kauf `GameFlags.SetFlag($"learned_{itemId}")` gesetzt (siehe "Training" unten) — taucht
  deshalb nie in der Verkaufen-Spalte auf.
- `QuestDefinition.RewardGold` zusätzlich zu `RewardItemIds` möglich (`QuestManager.CompleteQuest`).

## Training (`skill`-Items + `Player.IsComboUnlocked`)

- Kein eigenes Trainings-UI — Lehrer sind einfach Händler (siehe "Handel"), deren
  `ShopItemIds` ein `skill_*`-Item enthalten (`Data/Items/skill_onehanded_combo.json`,
  `skill_twohanded_combo.json`). Kauf setzt `learned_onehanded_combo`/`learned_twohanded_combo`.
- `Player.PerformAttack` prüft über `IsComboUnlocked(weapon.WeaponCategory)`, ob das passende
  Flag gesetzt ist, bevor der Links-/Rechts-Combo-Bonus greift (siehe "Kampf" oben).
  `ItemDefinition.WeaponCategory` (`onehand`/`twohand`/`bow`) steuert, welches Flag zählt — Bögen
  haben aktuell keine Combo-Mechanik.
- Neuer Trainer/neue Fähigkeit hinzufügen = neues `skill_*`-Item anlegen, in `ShopItemIds` eines
  NPCs eintragen, im entsprechenden Code (aktuell nur `Player.IsComboUnlocked`) den Flag-Namen
  auswerten.

## Proaktive NPC-Interaktion (`scripts/Interaction/`, `scripts/World/GuardCheckpoint.cs`)

Bisher lösten NPCs Dialoge nur über `E`/`Npc.Interact()` aus. Zwei neue Bausteine für NPCs, die
selbst die Initiative ergreifen:

- `ProximityDialogue` (Area3D, `Objects/Triggers/ProximityDialogue.tscn`): startet beim Betreten
  automatisch einen Dialog (`DialogueRunner.Start(...)`), einmalig über ein eigenes
  `TriggerFlagId`. Ergänzt einen NPC, ersetzt `Npc.Interact()` nicht — beide können auf dieselbe
  `DialogueId` zeigen (z.B. Bettler in Nethora: proaktive Ansprache beim Vorbeilaufen, danach
  normal per `E` weiter ansprechbar).
- `GuardCheckpoint` (Area3D + Kind-`StaticBody3D` "Barrier", `Objects/Triggers/
  GuardCheckpoint.tscn`): ohne `RequiredFlagId` bleibt die Barriere-`CollisionShape3D` aktiv
  (physisch blockiert) und eine (halbtransparente, rote) `MeshInstance3D` zeigt sie an; zusätzlich
  löst das Betreten der Trigger-Area3D einen erzwungenen Dialog mit der Wache aus. Sobald das
  Flag gesetzt wird (`GameFlags.FlagChanged`), gibt die Barriere automatisch den Weg frei.
  Beispiel: `BurgtorWache` in `Nethora.tscn` sperrt den Weg zu König Aldemar, bis
  `quest_completed_quest_der_bote` (Cassian-Gespräch) gesetzt ist.
  **Bekannte Einschränkung**: die Barriere ist eine einzelne breite Wand (16 Einheiten), kein
  vollständiger Zaun um die Zone — auf offenem, flachem Gelände könnte ein Spieler theoretisch
  weit genug außen herumlaufen. Für eine Blockout-Welt ohne echte Wege/Mauern ist das eine
  bewusste Einschränkung (siehe "Bewusste Vereinfachungen" unten), keine vollständige Lösung.

## Stats & Magie (`scripts/Magic/`)

- Jeder Charakter hat `Health` (Leben), `Strength` (Stärke) und `Dexterity` (Geschicklichkeit).
- Waffen skalieren über `Item.ScalingStat`: Nahkampfwaffen mit `strength`, Bögen mit `dexterity`.
- Drei Schulen (`SpellDefinition.School`, `Data/Spells/*.json`, siehe
  `doc/konzept/Gameplay/Magiesystem.md`): **arcane** (alte Kampfmagie), **blood** (Blutmagie),
  **divine** (göttliche Magie/Gebete). Alle nutzen `CharacterStats.CurrentHealth` als gemeinsame
  Ressource statt eines separaten Mana-Werts (`HealthCost` wird sofort beim Wirken abgezogen).
  - **arcane**: sofortiger Treffer auf den nächsten Gegner in `Range`, kein echtes Projektil.
  - **blood**: befristeter Flächeneffekt (`spell_fluchsiegel`) — zieht periodisch Leben von allen
    Gegnern in `Range` zum Wirkenden. Nur ein aktiver Fluch gleichzeitig (v1-Einschränkung).
  - **divine**: `CastTime > 0` sperrt Bewegung während der Kanalzeit (`SpellCaster.IsChanneling`,
    von `Player._PhysicsProcess` abgefragt wie der Dialog-Freeze), Effekt (z.B. Heilung) wird nach
    Ablauf auf einmal angewendet statt tatsächlich tropfenweise über Zeit.
  - `SpellCaster` sitzt als Sibling von `Stats`/`Inventory`/`Equipment` am Player, feste 3
    Zauber-Slots (`KnownSpellIds`) statt Zauberbuch/Lern-UI — Tasten 1/2/3
    (`cast_spell_1/2/3`).

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
- Kampf: kein echtes Timing-Fenster/Windup (kein Animationssystem, nur simples Links-/Rechts-
  Abwechseln als Combo-Grundlage), Gegner greifen ohne eigene Hitbox direkt an, kein echtes Parry,
  kein visuelles/akustisches Trefferfeedback.
- Ausrüstung: manuelles Equip nur über den Inventar-Knopf (kein Drag&Drop, keine Slot-Grafik),
  Runen sind bewusst nicht ausrüstbar (Zauber-Reagenz statt Ausrüstung).
- Magie: feste 3 Zauber-Slots statt Zauberbuch/Lern-UI, arkane Zauber ohne echtes Projektil,
  Blutmagie erlaubt nur einen aktiven Fluch gleichzeitig, göttliche Heilung wird nach der
  Kanalzeit auf einmal statt tropfenweise angewendet.
- Handel/Training: kein Drag&Drop, keine Mengenauswahl beim Kaufen/Verkaufen (immer 1 Stück pro
  Klick bei Consumables/Material, Waffen/Rüstung ohnehin nicht stapelbar), Training ist ein reiner
  Gold-Kauf ohne Lernpunkte-/XP-System aus `doc/konzept/Gameplay/Progression.md`.
- Zugangskontrolle (`GuardCheckpoint`): einzelne breite Barriere statt vollständigem Zaun um die
  Zone, siehe Einschränkung im Abschnitt "Proaktive NPC-Interaktion".
- Weltgröße: Die vier Gebiete sind größer als ursprünglich, aber weiterhin Blockout-Boxen ohne
  Wegenetz/Straßen — "größer" heißt mehr Abstand und mehr Unterbereiche, nicht mehr Detailgrad.
  Zwischen den Gebieten liegt offenes, leeres Gelände (kein gestalteter Übergangsweg).
- Kampf/Magie/Handel/Training sind ausschließlich `dotnet build`-geprüft, nicht im Godot-Editor
  gespielt (siehe `doc/TODO.md`) — echter Playtest steht noch aus.

## Lektion: `Free()` vs. `QueueFree()`

Erster echter Laufzeit-Absturz (nicht durch `dotnet build` auffindbar): Das inzwischen entfernte
`ZoneManager.LoadZoneContent` rief `child.Free()` (sofortiges, synchrones Löschen) auf allen
Kindern eines Nodes auf — aufgerufen aus dem `BodyEntered`-Signal-Handler eines dieser Kinder
selbst (`ZoneTransition`). Das Objekt, dessen eigener Callback noch auf dem Call-Stack lag, wurde
mitten in der Ausführung zerstört → Absturz. **Merksatz für dieses Projekt**: Nodes niemals per
`Free()` löschen, wenn der Aufruf (auch indirekt) aus einem Signal-Handler eines der zu
löschenden Nodes selbst stammen könnte — dafür immer `QueueFree()` (verschiebt das Löschen ans
Frame-Ende). Aktuell gibt es keinen Laufzeit-Code mehr, der Nodes zur Laufzeit entfernt (die
Welt ist jetzt statisch, siehe "Welt" oben), aber falls das wieder gebraucht wird (Lade-Systeme,
Gegner-Despawn o.ä.): `QueueFree()` ist die Standardwahl, `Free()` nur wenn wirklich sofortige
Zerstörung außerhalb jeder Signalverarbeitung sichergestellt ist.
