# Magiesystem

<!--
Drei Arten laut Notizen: Alte/Kampfmagie (Feuer/Eis/Arkan), Blutmagie (kostet Leben, kann Leben
zurückgeben z.B. via Fluchsiegel), Göttliche Magie/Gebete (Stillstand nötig, Heilung/Buffs/
Dämonen-Attacken). Konkrete Zauber pro Art, Kosten/Balance, wie erlernt/freigeschaltet.
Ausgangsbasis: doc/informationen/Spielmechaniken.txt. Bereits als Grundregeln dokumentiert in
doc/architektur.md ("Stats & Magie") — dort auch der technische Anknüpfungspunkt.
-->

Grundregeln (göttlich vs. dämonisch, Leben als gemeinsame Ressource) bereits in
[`doc/architektur.md`](../../architektur.md#stats--magie) als Vorgabe für die Implementierung
festgehalten. Hier die inhaltliche Ausgestaltung der drei Schulen.

## Alte Magie / Kampfmagie

Elementarer Kampfzauber: Feuer, Eis, arkane/Energie-Magie. Kostet Leben direkt beim Wirken, dafür
sofort einsetzbar mitten im Kampf (kein Stillstehen nötig) — die aggressive, riskante Schule.

**Erlernt** über das Freies-Lager-Rangsystem (Lehrling → Wissenssucher → Altmagier, siehe
[Fraktionen.md](../Welt/Fraktionen.md)), Lehrer ist Altmagier Torvald (siehe
[NPCs.md](../Charaktere/NPCs.md)). Konsequenz laut Lore: das Erlernen/Nutzen dieser Magie
schwächt ungewollt das Siegel des Erbauers (siehe
[Antagonisten.md](../Charaktere/Antagonisten.md)) — ein Balance-Hebel, der erzählerisch genutzt
werden kann (z.B. sichtbare Weltveränderung, je mehr der Spieler diese Schule nutzt).

Vorschlag für konkrete Zauber:
- **Feuerball** — mittlerer Schaden über Zeit (Nachbrennen), günstig im Lebenspreis.
- **Eisspeer** — hoher Einzelschaden, verlangsamt/staggert den Gegner zusätzlich.
- **Arkane Klinge** — kurzzeitige Waffenverzauberung (Rüstungsdurchdringung) statt Direktschaden.

## Blutmagie

Untergruppe der alten Magie, deutlich extremer: höherer Lebenspreis, kann aber Leben
zurückgeben, wenn geschickt eingesetzt — nicht als sofortige Heilung, sondern als Umweg über den
Gegner. Beispiel laut Ausgangsnotizen: ein **Fluchsiegel**, das die Hälfte des aktuellen Lebens
kostet, dafür pro Sekunde von jedem Gegner im Siegel einen festen Lebensbetrag an den Wirkenden
zurückzieht — Netto-Gewinn nur bei mehreren Gegnern gleichzeitig, Netto-Verlust bei einem
einzelnen. Konkrete Siegel/Runen siehe [Runen.md](../Items/Runen.md).

Erzählerisch ist Blutmagie die gefährlichste Schule — [Ulf](../Charaktere/Antagonisten.md) zeigt
als bekämpfbarer Zwischengegner, was bei zu sorglosem/gierigem Einsatz passiert. Balance-mäßig
sollte Blutmagie dadurch spürbar riskanter sein als reine Kampfmagie (höhere Sofortkosten, aber
höheres Schadens-/Rückgewinnpotenzial), nicht einfach eine bessere Version derselben Schule.

## Göttliche Magie / Gebete

Lange Kanalzeit, während der Bewegung komplett gesperrt ist — dadurch grundsätzlich ungeeignet für
Einsatz mitten im offenen Schlagabtausch, eher für Vorbereitung/Nachsorge außerhalb akuter
Gefechte gedacht: Heilung, Buffs (z.B. mehr Rüstung/Schaden für begrenzte Zeit), sowie Angriffe,
die speziell gegen Dämonen wirken (thematisch passend gegen vom Erbauer beeinflusste Gegner).

**Erlernt** über das Nethora-Rangsystem (Ritter → Mönch → Priester), Lehrer ist Priester Ansel für
Gebete allgemein, Mönch Wendelin für den Einstieg (siehe [NPCs.md](../Charaktere/NPCs.md)).

Vorschlag für konkrete Gebete:
- **Segnung** — heilt über Zeit, kein Soforteffekt (passend zur langen Kanalzeit).
- **Rüstung des Glaubens** — temporärer Rüstungsbonus.
- **Bann gegen das Böse** — Schadenszauber, nur gegen Dämonen/vom Erbauer korrumpierte Gegner
  wirksam, gegen normale Menschen/Tiere wirkungslos oder stark abgeschwächt.

## Die Erweckungsrituale der Erleuchteten (Sonderfall)

Keine eigene "spielbare" Magieschule im klassischen Sinn, sondern die Rituale der Sekte
(Erwecker → Seher, gelehrt von Voss, siehe [NPCs.md](../Charaktere/NPCs.md)) — nutzt laut Lore
direkt die Magie des Erbauers. Ob der Spieler diese Schule tatsächlich erlernen kann oder sie rein
narrativ/gegnerseitig bleibt (z.B. nur in Vox Aedificares' Ritualen sichtbar), ist noch offen.

## Offene Punkte

- Konkrete Zahlenwerte (Lebenskosten, Schaden, Kanalzeiten) folgen mit der tatsächlichen
  `SpellCaster`-Implementierung (`scripts/Magic/`, siehe `doc/architektur.md`).
- Ob es einen Attributwert für Magie gibt (aktuell nur `Strength`/`Dexterity` in
  `CharacterStats.cs`) oder Magie rein über Health/Ausrüstung/Zauberliste läuft.
