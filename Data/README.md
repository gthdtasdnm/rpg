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
