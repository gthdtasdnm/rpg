# Data/ — Spielinhalte als JSON

Wird beim Start vom Autoload `GameData` (`scripts/Data/GameData.cs`) eingelesen. **Neuer Inhalt =
neue Datei, kein Code.** Godot muss dafür nicht neu gestartet werden, das Spiel schon.

## Regeln

- **Dateiname = `<typ>_<id>.json`**, und das Feld `"id"` **muss** dem Dateinamen ohne Präfix und
  Endung entsprechen (`npc_gerald.json` → `"id": "npc_gerald"`). Nachgeschlagen wird immer über
  `"id"`, nicht über den Dateinamen.
- **Dateien mit `_` am Anfang werden ignoriert** (`_vorlage.json`, `_entwurf_xy.json`). So kannst
  du unfertige Inhalte im Ordner liegen lassen, ohne dass sie ins Spiel geraten.
- Feldnamen sind **camelCase**, Groß-/Kleinschreibung ist egal (`maxHealth` = `MaxHealth`).
- Nicht gesetzte Felder bekommen den Default aus der C#-Klasse — **jede Datei muss nur das
  enthalten, was vom Default abweicht.**

## Ordner und ihre Schemas

Die verbindliche Feldliste steht jeweils in der C#-Klasse (dort auch die erlaubten Werte für
`type`, `school`, `damageType` … als Kommentar). Kopiervorlage ist die `_vorlage.json` im Ordner.

| Ordner | Klasse | Wofür |
|---|---|---|
| `Characters/` | `scripts/Data/CharacterDefinition.cs` | Spieler, NPCs **und** Gegner (gleiche Basis: Leben/Stärke/Geschick) |
| `Items/` | `scripts/Data/ItemDefinition.cs` | Waffen, Rüstung, Schilde, Tränke, Material, Questgegenstände, Trainings-Einträge |
| `Quests/` | `scripts/Data/QuestDefinition.cs` | Quests mit Zielen und Belohnungen |
| `Spells/` | `scripts/Data/SpellDefinition.cs` | Zauber |

**Dialoge liegen nicht hier**, sondern als `.dialogue`-Dateien in `Dialogues/` — siehe
`Dialogues/README.md`.

## Items brauchen nur ein Modell, kein Icon

Ein Item hat **ein** 3D-Modell (`"model": "res://Assets/..."`), und das genügt für alles:

| Wo | Wer macht das |
|---|---|
| liegt in der Welt | `Objects/world_item.tscn` (`scripts/Items/ItemPickup.cs`) |
| hängt beim Ausrüsten am Spieler | `scripts/Combat/EquipmentVisuals.cs` |
| ist das Inventar-Icon | `scripts/UI/ItemIcons.cs` |

**Icons werden nicht gemalt, sondern gerendert.** `ItemIcons` (Autoload) stellt das Modell in
einen eigenen kleinen SubViewport, rahmt es automatisch und rendert **einmal**; danach steht die
Textur und kostet nichts mehr. Längliche Sachen (Schwerter, Stäbe, Bögen) werden dabei automatisch
so gedreht, dass sie diagonal im Feld liegen und man ihre breite Seite sieht — die Modellpakete
legen ihre Waffen nämlich nicht alle auf dieselbe Achse. Kompakte Sachen (Trank, Buch, Schild)
bleiben aufrecht stehen.

Wenn die Automatik bei einem Item danebenliegt: `"iconAutoOrient": false` und mit
`iconYaw`/`iconPitch`/`iconRoll` (Grad) selbst drehen, `iconZoom` > 1 zoomt näher heran. Ein
fertiges Bild gewinnt immer: `"icon": "res://..."`. Ohne Modell zeigt das Inventarfeld ein
Namenskürzel — kaputt geht nichts.

**Ein Item in die Welt legen:** `Objects/world_item.tscn` in die Szene ziehen, `ItemId` eintragen,
hinstellen. Modell und Kollision entstehen von selbst, und weil das Script ein `@tool` ist, sieht
man das Item schon im Editor. Der Knoten-Ursprung liegt dabei auf der **Unterkante** des Modells —
auf den Boden setzen genügt.

## Währung

**Silber** ist die einzige Währung — keine Kupfer/Gold-Staffelung. Das Feld `price` in
`Data/Items/*.json` ist immer ein Silberbetrag; verkauft wird für die Hälfte. Im Code:
`Inventory.Silver`, im Dialog `Dialog.GetSilver()` / `GiveSilver()` / `TakeSilver()`.

## Fähigkeiten (Lehrer)

Etwas, das man lernt statt es zu besitzen, ist ein **Item vom Typ `skill`** in `Data/Items/` —
mit `price` als Lehrgeld. Beim Lernen wandert nichts ins Inventar, es wird ein Flag
`learned_<id>` gesetzt. Beispiele: `skill_bogen`, `skill_haeuten`.

## Namenskonvention für IDs

| Präfix | Beispiel |
|---|---|
| `npc_` | `npc_gerald` |
| `enemy_` | `enemy_wolf` |
| `weapon_` / `armor_` / `shield_` | `weapon_ordensklinge` |
| `potion_` / `item_` / `rune_` | `potion_heiltrank` |
| `quest_` | `quest_der_bote` |
| `spell_` | `spell_feuerball` |

Flags folgen einem eigenen Muster (`talked_<id>`, `quest_started_<id>`, `quest_completed_<id>`,
`entered_<ort>`, `learned_<id>`) — siehe `scripts/World/GameFlags.cs`.
