# Dialogues/ — Gespräche

`.dialogue`-Dateien für das **Dialogue-Manager-Addon**. Im Godot-Editor doppelklicken → eigener
Editor mit Syntax-Hervorhebung; Fehler werden beim Speichern angezeigt, nicht erst im Spiel.

**Einen NPC mit einem Dialog verbinden:** `World/npc.tscn` in die Szene ziehen, im Inspector
`Dialogue File` auf die `.dialogue`-Datei setzen und `Dialogue Title` auf den Einstiegstitel
(Standard `start`). Kein Code.

## Wie ein Gespräch anfängt

Zwei Wege — beide landen im selben Dialog:

**1. Der Spieler spricht an** (Standard): `E` drücken, wenn der Name eingeblendet wird.

**2. Der NPC hält den Spieler an** (Gothic-Stil): am NPC im Inspector `Greet On Approach`
anhaken. Sobald der Spieler in den Umkreis kommt, startet der Dialog von selbst, der Spieler
bleibt stehen und der NPC dreht sich zu ihm.

| Feld | Bedeutung |
|---|---|
| `Greet On Approach` | Schaltet das Ansprechen ein (aus = klassisch nur per `E`) |
| `Greet Radius` | Ab welcher Entfernung, in Metern (Standard 6) |
| `Greet Only Once` | An: nur beim allerersten Mal, gemerkt über das Flag `greeted_<id>` (bleibt im Spielstand). Aus: jedes Mal, wenn der Spieler den Umkreis neu betritt |
| `Turn To Player` | NPC dreht sich beim Ansprechen zum Spieler (weich, `Turn Speed`) |

Schreib den Dialog dann so, dass die erste Zeile zum Überfallenwerden passt — Beispiel aus
`falk.dialogue`: *„Halt an, Freundchen. Wo willst du hin?"*

**Sparsam einsetzen.** Wenn jeder NPC von selbst anfängt, kommt der Spieler nirgends mehr
vorbei. Gedacht für Wachen, Wegelagerer und Werber.

**Den Weg wirklich versperren**: `Objects/GuardCheckpoint.tscn` platzieren
(`scripts/World/GuardCheckpoint.cs`). Solange das Flag in `Required Flag Id` nicht gesetzt ist,
blockiert eine unsichtbare Wand den Durchgang und beim Anlaufen startet der Dialog aus
`Guard Dialogue File`. Sobald das Flag gesetzt wird — z.B. per `do Dialog.SetFlag(…)` in einem
Gespräch — verschwindet die Sperre von selbst.

## Syntax — das Wichtigste

```
~ start                          Sprungmarke. "start" ist der übliche Einstieg.

Halvor: Text, den er sagt.       Sprecher vor dem Doppelpunkt.

- Antwort des Spielers           Auswahlmöglichkeit
	Halvor: Reaktion darauf.     eingerückt (Tab!) = was passiert, wenn man sie wählt
	=> fragen                    Sprung zu einer anderen Marke

=> END                           Gespräch beenden
=>< fragen                       Hinspringen und danach hierher zurückkehren
```

**Wichtig:** Eingerückt wird mit **Tabs**, nicht mit Leerzeichen. Zeilen mit `#` sind Kommentare.

## Das Spiel ansprechen: `Dialog.…`

Alles, was ein Dialog im Spiel auslösen oder abfragen kann, geht über den Autoload `Dialog`
(`scripts/Dialogue/DialogueBridge.cs`). Neue Möglichkeiten = dort eine neue öffentliche Methode.

| Aufruf | Wirkung |
|---|---|
| `do Dialog.SetFlag("id")` | Merkt sich etwas dauerhaft (kommt in den Spielstand) |
| `do Dialog.ClearFlag("id")` | Flag wieder löschen |
| `Dialog.HasFlag("id")` | Abfrage in einer Bedingung |
| `do Dialog.StartQuest("quest_id")` | Quest aus `Data/Quests` starten |
| `do Dialog.CompleteQuest("quest_id")` | Quest abgeben |
| `Dialog.IsQuestStarted("id")` / `IsQuestCompleted("id")` | Abfrage |
| `Dialog.IsQuestReady("id")` | Alle Ziele erfüllt, aber noch **nicht abgegeben** — die Bedingung für die „Ich hab's erledigt"-Antwort |
| `do Dialog.OpenShop("npc_id")` | Händlerfenster öffnen (Sortiment: `shopItemIds` im Charakter-JSON) |
| `do Dialog.GiveItem("id", n)` / `TakeItem("id", n)` | Gegenstände geben/wegnehmen |
| `Dialog.HasItem("id", n)` / `CountItem("id")` | Abfrage |
| `do Dialog.GiveSilver(n)` / `TakeSilver(n)` | **Silber** ist die einzige Währung im Spiel |
| `Dialog.GetSilver()` / `HasSilver(n)` | Abfrage |
| `do Dialog.Learn("skill_id")` | Fähigkeit lehren: zieht den Preis aus dem Item-JSON ab, setzt `learned_<id>` |
| `Dialog.HasLearned("skill_id")` / `GetPrice("id")` | Abfrage |

### Bedingungen

```
- Nur zu sehen, wenn das Flag NICHT gesetzt ist [if not Dialog.HasFlag("x")]
- Nur zu sehen, wenn die Quest läuft [if Dialog.IsQuestStarted("quest_x")]

if Dialog.HasFlag("x")
	Halvor: Kennen wir uns nicht schon?
else
	Halvor: Wer bist du?
```

## Muster: ein Lehrer

Eine Fähigkeit ist ein Item vom Typ `skill` in `Data/Items/` — dort steht auch der Preis. Gelernt
wird nicht in den Rucksack, sondern als Flag `learned_<id>`. Vollständig in `falk.dialogue`:

```
- Bring mir den Bogen bei. [if not Dialog.HasLearned("skill_bogen")]
	Falk: Hundert Silber.
	=> bogen_lernen

~ bogen_lernen
- Hier, hundert Silber. [if Dialog.HasSilver(100)]
	do Dialog.Learn("skill_bogen")
	Falk: Na also.
	=> fragen
- So viel habe ich nicht. [if not Dialog.HasSilver(100)]
	Falk: Dann komm wieder, wenn du was hast.
	=> fragen
```

Die beiden Antworten schließen sich gegenseitig aus — es ist immer genau eine sichtbar.

## Muster: ein Frage-Menü ("frag mich was")

Der Standardaufbau, siehe `halvor.dialogue`: eine Marke `~ fragen` mit lauter Antwortzeilen,
und **jedes Thema springt am Ende mit `=> fragen` zurück**. Untermenüs sind einfach eine
weitere Marke, die auf `=> fragen` zurückspringt.

Neues Thema hinzufügen = eine `- Zeile` mit eingerücktem Block. Sonst nichts.

## Konventionen

- **Eine Datei pro Charakter**, benannt nach ihm: `halvor.dialogue`.
- Der Name vor dem Doppelpunkt ist reiner Anzeigetext — er muss nicht zum `CharacterId` passen,
  sollte aber gleich geschrieben sein wie in `Data/Characters/`.
- Flag-Namen wie im restlichen Projekt (siehe `scripts/World/GameFlags.cs`):
  `talked_<id>`, `quest_started_<id>`, `quest_completed_<id>`, `entered_<ort>`, `learned_<id>`.
  Wissensflags frei benennbar, z.B. `weiss_von_fraktionswahl`.
- `talked_<CharacterId>` setzt `Npc.cs` beim Ansprechen **automatisch** — nicht selbst setzen.

## ⚠️ Falle: „hab ich den schon mal getroffen?"

`talked_<id>` eignet sich **nicht**, um die Erstbegrüßung von späteren Gesprächen zu trennen:
`Npc.cs` setzt das Flag, *bevor* der Dialog startet — beim allerersten Gespräch ist es also schon
gesetzt. Stattdessen ein eigenes Flag am Ende der Begrüßung setzen:

```
~ start

if Dialog.HasFlag("halvor_begruessung_gehabt")
	Halvor: Du schon wieder. Was willst du?
	=> fragen

Halvor: Haben sie wieder eine arme Sau hier reingeschmissen?
do Dialog.SetFlag("halvor_begruessung_gehabt")

=> fragen
```

## Wen der Charakter kennt

Bevor du einen Dialog schreibst: Ton, Haltung und Sprechweise der Figur stehen in
[`doc/konzept/Charaktere/Steckbriefe.md`](../doc/konzept/Charaktere/Steckbriefe.md). Dort steht
auch, **was die Figur nicht weiß** — der häufigste Fehler ist ein NPC, der Dinge ausplaudert, die
er gar nicht wissen kann.

## Aussehen der Dialogbox

`UI/DialogueBalloon.tscn` (Layout/Theme) + `scripts/UI/DialogueBalloon.cs` (Verhalten). Beides
sind Kopien des Addon-Beispiels, damit ein Addon-Update sie nicht überschreibt. Eingestellt wird
die Box über `runtime/balloon_path` in `project.godot`.
