# Anforderungen

Grobe Sammlung an Themen, die für das Spiel noch durchdacht und umgesetzt werden müssen.

## Interaktionen
- [x] Interaktionssystem mit Objekten/NPCs ("E zum Interagieren")
- [x] Aufheben/Einsammeln von Gegenständen in der Welt

## Andere Charaktere
- [x] NPCs (datengetrieben per JSON, ~19 benannte + 8 generische Rollen-NPCs über 3 Fraktionen +
  Gerald — generische NPCs wie "Bauer"/"Bandit"/"Bettler" tragen die Rolle statt eines Eigennamens
  als `name`, siehe `doc/TODO.md` Milestone 8)
- [x] Gegner/Feinde (`scripts/Combat/Enemy.cs`, 3 Typen: organisch/gepanzert/Wegelagerer)
- [x] Kampfsystem (Nahkampf, Blocken, Stagger, Schadensart-Resistenzmatrix, Links-/Rechtsklick-
  Combo an Waffentraining gekoppelt — siehe `doc/TODO.md` für bewusste v1-Vereinfachungen,
  **ungespielt getestet**)

## Welt / Map
- [x] Grobe Weltkarte / Aufteilung in Gebiete (4 Gebiete: Ankunft, Nethora, Freies Lager,
  Sektentempel, jeweils in Unterbereiche gegliedert (Burg/Ordenssitz/Marktviertel/Wohnviertel in
  Nethora usw.), Blockout-Geometrie, kein Kunststil)
- [x] **Durchgehende offene Welt statt Zonenladen**: alle 4 Gebiete liegen gleichzeitig geladen
  in einer Szene (`Level/World.tscn`), an unterschiedlichen Weltkoordinaten, ohne Übergänge/
  Ladepunkte — man läuft einfach hin. (Ursprünglich als Zonen-Wechsel-System gebaut, auf
  Nutzerwunsch zu einer echten offenen Welt umgebaut, siehe `doc/architektur.md` "Welt".)
- [x] Weitere Orte aus `Orte.md` (Küstenregion, Mine, Alte Kathedrale, 4 weitere Erbauer-Tempel)
  als angedeutete Prop-Cluster in der Ankunft-Gegend (siehe `doc/TODO.md` Milestone 7)
- [x] Zugangskontrolle (`GuardCheckpoint`): Wache versperrt physisch den Weg zur Burg in Nethora,
  bis eine Bedingung erfüllt ist (siehe `doc/architektur.md`)

## Handel & Training
- [x] Währung (`Inventory.Gold`), Kaufen/Verkaufen-UI (`Hud` ShopPanel), Händler-Sortimente
  (`CharacterDefinition.ShopItemIds`) — siehe `doc/architektur.md` "Handel"
- [x] Waffentrainer (Einhand/Zweihand) schalten die Kampf-Combo gegen Gold frei
  (`skill_*`-Items + `learned_*`-Flags) — siehe `doc/architektur.md` "Training"

## Dialoge
- [x] Dialogsystem (JSON-Dialogbäume, UI mit Auswahlmöglichkeiten)
- [x] Erste Testdialoge mit NPCs (Gerald)

## Items
- [x] Item-System (Datenstruktur, Typen: Waffen, Verbrauchsgüter, Quest-Items)
- [x] Inventar-UI (Panel, Taste `I`)

## Quests
- [x] Quest-System (Ziele, Fortschritt, automatischer Abschluss)
- [x] Erste Testquest ("Kräuter für Gerald")
- [x] Questlog-UI (Panel, Taste `L`)

## UI / Meta
- [x] Lebensbalken
- [x] Pause-Menü (Fortsetzen/Speichern/Laden/Beenden)
- [x] Speichersystem (ein Slot, `user://savegame.json`)

## Story
- [x] Grundidee/Setting überlegen
- [x] Hauptkonflikt / roter Faden (siehe `doc/konzept/Story/Haupthandlung.md`)
- [x] Grober Handlungsbogen (Akt 1–3 + 4 mögliche Enden, siehe `doc/konzept/Story/Haupthandlung.md`)
- [x] Akt-1-Questkette Teil 1 tatsächlich umgesetzt (`quest_der_bote` → `quest_die_nachricht`,
  Cassian/Aldemar in der Nethora-Zone)

## Magie
- [x] Regeln festgelegt und dokumentiert (siehe `doc/architektur.md`): göttlich vs. dämonisch,
  beide nutzen Leben als Ressource, dämonisch kostet sofort, göttlich braucht lange Kanalzeit
  + Bewegungssperre
- [x] Tatsächliche Cast-Logik (`scripts/Magic/SpellCaster.cs`), 4 Zauber über 3 Schulen —
  **ungespielt getestet**, siehe `doc/TODO.md` Milestone 3
