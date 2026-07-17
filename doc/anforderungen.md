# Anforderungen

Grobe Sammlung an Themen, die für das Spiel noch durchdacht und umgesetzt werden müssen.

## Interaktionen
- [x] Interaktionssystem mit Objekten/NPCs ("E zum Interagieren")
- [x] Aufheben/Einsammeln von Gegenständen in der Welt

## Andere Charaktere
- [x] NPCs (datengetrieben per JSON, erster NPC "Gerald")
- [ ] Gegner/Feinde
- [ ] Kampfsystem

## Welt / Map
- [ ] Grobe Weltkarte / Aufteilung in Gebiete
- [ ] Übergänge zwischen Leveln/Zonen

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
- [ ] Grundidee/Setting überlegen
- [ ] Hauptkonflikt / roter Faden
- [ ] Grober Handlungsbogen

## Magie
- [x] Regeln festgelegt und dokumentiert (siehe `doc/architektur.md`): göttlich vs. dämonisch,
  beide nutzen Leben als Ressource, dämonisch kostet sofort, göttlich braucht lange Kanalzeit
  + Bewegungssperre
- [ ] Tatsächliche Cast-Logik (`scripts/Magic/`) — zurückgestellt, bis es Kampf/Gegner gibt
