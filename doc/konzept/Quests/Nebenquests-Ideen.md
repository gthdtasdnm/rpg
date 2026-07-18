# Nebenquest-Ideen

<!--
Lose Sammlung an Nebenquest-Ideen (müssen noch nicht ausgearbeitet sein). Ein Satz Idee reicht
erstmal, Ausbau später. Technisch: Objective-Typen aktuell nur collect_item, siehe
doc/architektur.md für Erweiterung um weitere Typen (z.B. talk_to_npc, reach_location).
-->

Größtenteils direkt aus den in [NPCs.md](../Charaktere/NPCs.md) angelegten Questgeber-Hinweisen
abgeleitet — ein Satz pro Idee reicht laut Vorlage, Ausbau folgt bei Bedarf.

## Nethora

- **Oswin** — hilft bei seiner Bewährungsaufgabe als Vigil, unklar wobei genau (offen).
- **Tobias** — wird von älteren Rittern schikaniert; der Spieler kann vermitteln oder für ihn
  Partei ergreifen.
- **Fabian** — ein verlorener Gegenstand an der Torwache muss wiedergefunden werden.
- **Roderich** — bietet ein Turnier/Wettkampf-Nebenquest an, reiner Kampf-Content ohne Story-
  Gewicht.
- **Konrad** — sieht den Spieler als Konkurrenz; könnte in eine kleine Rivalitäts-/Sabotage-Quest
  münden.
- **Ulrich** — Beschaffung geweihter Gegenstände für Reisende, die er segnen soll.

## Freies Lager

- **Bram** — sucht Verstärkung für einen gefährlichen Auftrag (Details offen).
- **Piet vs. Ragnar** — konkurrieren um dieselben "Aufträge"; der Spieler kann sich für einen von
  beiden entscheiden, was den anderen verärgert.
- **Brandt** — Comic-Relief-Figur, gerät ständig in kleine Schwierigkeiten, aus denen ihn der
  Spieler wiederholt raushauen kann.
- **Nils** — sammelt Ruinenfragmente für Otbert; einfache `collect_item`-Quest, technisch schon
  heute umsetzbar.
- **Otbert vs. Vance** — rivalisieren um dieselben Funde; ähnlich wie Piet/Ragnar eine
  Entscheidungsquest mit Konsequenz für die Beziehung zum jeweils anderen.

## Die Erleuchteten

- **Reth** — jagt einen vermuteten Spion aus Nethora; der Spieler kann den Spion decken oder
  verraten.
- **Nym** — stellt dem Spieler eine gefährliche Mutprobe zur Aufnahme.
- **Fenwick** — zweifelt an seinem Beitritt; Ausstiegsquest, siehe
  [Charakterboegen.md](../Story/Charakterboegen.md).
- **Vael** — kennt Gerüchte über den König aus seiner Zeit als Palastdiener; Info-Quest, die
  Licht auf König Aldemars Vorgeschichte wirft.

## Unabhängige/Verstreute Charaktere

- **Aldous** (Fährmann) — verlangt Wegzoll; kleine Verhandlungs- oder Bezahl-Quest an der
  Flussquerung.
- **Emrich** (Schatzsucher) — gerät in Konkurrenz zu den Wissenssuchern des Freien Lagers; könnte
  zu einem Wettlauf um denselben Fund führen.
- **Sarge** (Totengräber/Plünderer) — bietet seltene Fundstücke, evtl. gegen eine moralisch
  zweifelhafte Gegenleistung (z.B. Schweigen über eine Plünderung).
- **Morwen** (Hexer) — Kräuter/Flüche/Segen; mögliche Quest rund um Aberglauben, den Nethora
  offiziell verurteilt.
- **Galen** (Eremit) — ehemaliger Mönch, weiß überraschend viel über alte Kirchentexte; passt zu
  den Lore-Quests rund um Arthur/Wandbilder (siehe [Erkundung.md](../Gameplay/Erkundung.md)).
- **Kilian** (Deserteur) — wird von Nethora gesucht; Entscheidungsquest (ausliefern/schützen).
- **Hollis** (Geächteter) — ähnliches Schicksal wie der Spieler, nur länger her; könnte als
  Spiegel-Nebenquest dienen ("was aus mir hätte werden können").
- **Berthold** (geflohener Palastdiener) — erlebte Aldemars Zusammenbruch mit; Kontrastfigur zu
  Vael, der sich stattdessen der Sekte anschloss.
- **Brack** (Wilderer) — jagt illegal in Nethoras Wäldern; Konfliktpotenzial mit Nethora-treuen
  Spielern.
- **Edric** (Kriegsversehrter) — von der Stadt fallengelassen; mögliche Quest um eine
  überfällige Entschädigung/Anerkennung.
- **Thane** (Flüchtling) — auf der Flucht mit seinem Sohn; kleine Versorgungs-/Schutzquest.

## Technischer Hinweis

Ein Großteil dieser Ideen braucht Objective-Typen über `collect_item` hinaus (Gespräche,
Entscheidungen, Ortswechsel) — siehe `doc/architektur.md` ("Neue Quest") für die Erweiterung von
`QuestManager` um weitere `NotifyXyz`-Methoden, bevor diese als echte `Data/Quests/*.json`
angelegt werden können.
