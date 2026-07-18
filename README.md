# RPG

Ein düsteres Fantasy-RPG in der Stadt Nethora, die zum Schutz vor Dämonen von einer Barriere
umschlossen wurde — unter ihr schläft (und erwacht langsam) ein uralter, versiegelter Dämon,
"Der Erbauer". Drei Fraktionen (Nethora-Loyalisten, das freie Lager, die Erleuchteten-Sekte)
verfolgen dazu jeweils eigene, gegensätzliche Ziele. 3rd-Person-Steuerung, datengetriebene
Charaktere/Items/Quests/Dialoge, Kampf- und Magiesystem angelehnt an Gothic.

## Tech-Stack

- Godot 4.7 (.NET/C#, `net8.0`)
- Jolt Physics

## Projektstruktur

```
Characters/   Charakter-Szenen (Player, Npc, ...)
Level/        Level-Szenen (Zusammensetzung aus Maps/Objects/Characters)
Maps/         Umgebung/Terrain-Szenen
Objects/      Props, Items, Trigger
UI/           HUD und Menüs
scripts/      C#-Code, ein Ordner pro Kategorie (Namespace RPG.<Kategorie>)
Data/         JSON-Inhalte (Charaktere, Items, Quests, Dialoge) — additiv erweiterbar
doc/          Dokumentation, siehe unten
```

## Dokumentation

- [`doc/konzept/`](doc/konzept/README.md) — Spieldesign/Konzeption (Welt, Story, Charaktere,
  Gameplay, Items, Quests, ...). Hier trägst du deine kreativen Entscheidungen ein.
- [`doc/informationen/`](doc/informationen) — rohe Notizen/Ideen-Sammlung als Ausgangsbasis.
- [`doc/anforderungen.md`](doc/anforderungen.md) — Feature-Liste mit Fortschritt (Checkboxen).
- [`doc/architektur.md`](doc/architektur.md) — technische Konventionen (Ordnerschema,
  JSON-Schemas, Flag-System, Speichersystem, ...).
- [`doc/backlog/`](doc/backlog) — ein Eintrag pro Arbeitstag, was jeweils umgesetzt wurde.

## Starten

Projekt in Godot 4.7 (Mono/.NET-Version) öffnen und Play drücken. Für einen reinen Build-Check
ohne Editor: `dotnet build RPG.sln`.
