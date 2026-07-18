# Verbrauchsgüter & Questgegenstände

<!--
Tränke/Heilmittel, Kräuter/Zutaten (z.B. für Alchemie/Crafting?), Questgegenstände pro Quest.
Landet am Ende als JSON in Data/Items/ (type: consumable/quest), siehe doc/architektur.md.
-->

## Kräuter & Zutaten

Bereits als Testdaten vorhanden: `herb_red` (siehe `doc/backlog/2026-07-18.md`,
Quest "Kräuter für Gerald"). Weitere Kräuter/Zutaten als Sammelressource für Tränke und
Ritualzutaten, unabhängig von Fraktion in der Wildnis auffindbar — passend zu unabhängigen
Figuren wie dem Hexer Morwen (siehe [NPCs.md](../Charaktere/NPCs.md)):

| Item | Verwendung |
|---|---|
| Rotes Kraut (`herb_red`) | Basis-Heiltrank, Testdaten bereits vorhanden |
| Bittermoos | Zutat für Blutmagie-Zubereitungen (Skarn, Sekten-Händler) |
| Reine Quellblüte | Zutat für göttliche Segnungen (Meinhard, Nethora) |

## Tränke

| Item | Effekt | Herkunft |
|---|---|---|
| Heiltrank | stellt Leben wieder her | Meinhard (Nethora), Doyle (Freies Lager, Schwarzmarkt) |
| Ausdauertrank | kurzzeitiger Kampfbonus | Doyle, Curran |
| Segenswasser | kleiner temporärer Rüstungsbonus, geweiht | Ulrich (Nethora) |

## Questgegenstände (Hauptquestreihe)

Direkt aus der bestehenden Lore ableitbar, siehe
[Haupthandlung.md](../Story/Haupthandlung.md):

| Item | Quest-Kontext |
|---|---|
| Königsbrief | Ausgangs-Questitem: die Nachricht, dass der König Nethora aufgegeben hat. Übergabe an Ordensmeister Cassian oder König Aldemar beendet Akt 1. |
| Wandbild-Abdruck / Kohleskizze | Aus den Ruinen/Katakomben geborgene Abzeichnung eines Arthur-Wandbilds — Lore-Beweisstück, siehe [Erkundung.md](../Gameplay/Erkundung.md). |
| Siegel-Fragment | Beweisstück aus einem der fünf Erbauer-Tempel, dass der Erbauer real ist. |

## Fraktions-Questgegenstände (Beispiele, vorläufig)

Passend zu bereits in [NPCs.md](../Charaktere/NPCs.md) angelegten Questgebern:

| Item | Quest-Kontext |
|---|---|
| Ruinenfragment | Sammelitem für Otbert/Nils (Freies Lager, Wissenssucher-Zweig) |
| Ritualzutat | Für Kael (Erleuchtete, Erweckungsritual) |
| Geweihter Gegenstand | Für Ulrich (Nethora, Beschaffungsquest) |
