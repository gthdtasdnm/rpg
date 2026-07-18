# Konzept

Struktur fürs Spieldesign, damit die Ideen aus [`doc/informationen/`](../informationen) geordnet
ausgearbeitet werden können. Jede Datei hat nur eine Überschrift + Leitfragen als Kommentar
(unsichtbar beim gerenderten Markdown) — einfach direkt reinschreiben.

Reihenfolge, in der es sich erfahrungsgemäß am einfachsten befüllen lässt: erst **Welt**, dann
**Story**, dann **Charaktere**, danach **Gameplay/Items/Quests** parallel (die hängen eng
zusammen), **Dialoge**/**Präsentation** zuletzt.

- **Welt/** — Setting, Fraktionen, Orte/Karte, Lore & Historie
- **Story/** — Haupthandlung, Charakterbögen, Themen & Ton
- **Charaktere/** — Spielercharakter, NPCs, Antagonisten
- **Gameplay/** — Kampfsystem, Magiesystem, Progression, Erkundung
- **Items/** — Waffen, Rüstung & Schilde, Verbrauchsgüter & Questgegenstände
- **Quests/** — Hauptquestreihe, Fraktionsquests, Nebenquest-Ideen
- **Dialoge/** — Ton & Sprache
- **Praesentation/** — Visueller Stil, Sound & Musik

Nicht jede Datei muss vor dem nächsten Entwicklungsschritt fertig sein — auch Stichpunkte oder
"weiß ich noch nicht" sind ein valider Zwischenstand. Wenn eine Kategorie fehlt, die du brauchst:
einfach eine neue Datei/Ordner nach dem gleichen Muster anlegen.

Was hier entschieden wird, wandert später in echte Spieldaten: Charaktere/Items/Quests/Dialoge
werden am Ende als JSON unter `Data/` angelegt (siehe [`doc/architektur.md`](../architektur.md)).
