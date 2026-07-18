# Runen

<!--
Physische Runensteine/Siegel-Gegenstände, die Blutmagie/alte Magie im Spiel greifbar machen —
Ergänzung zu Waffen.md/Ruestung-Schilde.md/Verbrauchsgueter-Questitems.md. Siehe Magiesystem.md
für die Regeln dahinter (Fluchsiegel, Lebenskosten).
-->

Ergänzt [Magiesystem.md](../Gameplay/Magiesystem.md): Blutmagie-Zauber wie das dort beschriebene
**Fluchsiegel** brauchen einen physischen Träger. Runen sind dieser Träger — geritzte
Steine/Fragmente, die ein Zauber-/Ritualmuster binden, statt reines Zahlenkonzept im
Zauberbuch zu bleiben. Optisch an die Rune/Zeichen-Symbolik der Erleuchteten angelehnt (siehe
[Visueller-Stil.md](../Praesentation/Visueller-Stil.md), violett-rote Ritualsymbolik), aber nicht
exklusiv für die Sekte — auch die Altmagier des Freien Lagers nutzen sie, da beide Seiten
letztlich dieselbe alte Magie anzapfen.

## Funktionsweise (Vorschlag)

Eine Rune wird beim Wirken verbraucht (technisch am ehesten `type: material`, siehe
`doc/architektur.md`) und bestimmt, welches Muster/welcher Effekt aktiviert wird — die Kosten
(Leben) und der Effekt selbst bleiben in den Zauberregeln aus
[Magiesystem.md](../Gameplay/Magiesystem.md) definiert, die Rune ist nur der Auslöser/Katalysator,
den man vorher finden/kaufen/herstellen muss.

## Konkrete Runen (Beispiele)

| Rune | Effekt | Beschaffung |
|---|---|---|
| Fluchsiegel-Rune | zieht Leben von Gegnern im Wirkbereich zum Wirkenden | Skarn (Erleuchtete, Ritualzutaten), selten auch bei Doyle (Freies Lager, Schwarzmarkt) |
| Splitterrune | verstärkt einen einzelnen Kampfzauber (Feuer/Eis/Arkan) einmalig | Torvald lehrt die Herstellung (Freies Lager) |
| Bannrune | statt Blutmagie eine gegen den Erbauer/seine Kreaturen gerichtete Variante — Brücke zur göttlichen Magie, evtl. nur über Nethora-Lore erreichbar | Fund in Ruinen/Katakomben, siehe [Erkundung.md](../Gameplay/Erkundung.md) |

## Offene Punkte

- Ob Runen frei kombinierbar sind (Baukastensystem) oder jede Rune ein fest definierter Zauber
  ist — Letzteres ist einfacher umzusetzen und passt eher zum aktuellen datengetriebenen
  Item-Schema.
- Herstellbarkeit durch den Spieler selbst (Crafting) vs. reines Sammeln/Kaufen — bisher ist im
  Konzept kein Crafting-System vorgesehen, siehe [Verbrauchsgueter-Questitems.md](Verbrauchsgueter-Questitems.md)
  (Kräuter aktuell nur für Tränke, kein Crafting-Menü).
