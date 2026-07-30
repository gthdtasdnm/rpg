# CLAUDE.md – Schnelleinstieg

Godot-RPG. **Sprache mit dem User: Deutsch.**

## Die vier Dokumente

| Datei | Inhalt |
|---|---|
| **`doc/Kapitelverlauf.txt`** | 🔴 **Vom Projektinhaber geschrieben, steht über allem.** Der verbindliche Spielablauf. Bei jedem Widerspruch gilt diese Datei, alles andere wird angepasst. Nicht ändern. |
| `doc/welt.md` | Lore, Fraktionen, Orte, Figuren, Ton, Stil |
| `doc/spielsysteme.md` | Kampf, Magie, Progression, Items — **fast alles noch Konzept** |
| `doc/technik.md` | Weltsysteme, Stellschrauben, Fallstricke, Leistung |

Mehr gibt es nicht. Alte Konzeptdateien (`doc/konzept/`, `roadmap.md`, `architektur.md`,
`backlog/`) wurden am 30.07. gelöscht — sie widersprachen dem Kapitelverlauf. Frühere Doku, die
sie nennt, ist veraltet; Git hat sie.

## Story in Kurzform

**Azazel** ist ein gefallener Engel, das manifestierte Böse. Vor 1000 Jahren band ihn der Erzengel
**Raphael** und warf ihn in den Abgrund unter Nethora — Engel lassen sich nicht töten, nur
verwahren. Über ihm liegt das **Siegel**: fünf **Türme** im Pentagramm, in jedem ein Engel, der
seinen Teil hält. Der **Orden der Custodes Profundi** („Wächter der Tiefe") bewacht das seit
tausend Jahren — inzwischen als leere Tradition, an die niemand mehr glaubt. **Nethora handelt im
Sinne Gottes, ohne es zu wissen.**

Die Sekte **Die Erleuchteten** betet Azazel als **„Aedificares", den Erbauer**, an und stärkt ihn
damit. Sein Einfluss verrohrt Menschen und Tiere. Seit 20 Jahren steht die **Barriere**. Zu
Spielbeginn gibt der König draußen auf und schickt den Spieler — einen **Verurteilten** — mit
einem Brief hinein, der das mitteilt.

Fünf Kapitel: **1** Ankunft, Brief, Fraktionswahl über ein Heiliges Buch · **2** **Abaddon**
(Skelett mit Krone, Engel des Abgrunds, König der Heuschrecken) in seiner Höhle töten und sein
Manuskript holen · **3** dadurch fließen die Plagen unkontrolliert, die fünf Turmengel werden
feindselig — man **heilt sie mit einem Gebet statt sie zu töten** und erhält je ein
Teleportstein-Fragment · **4** der Eremit **Salomo** setzt sie zusammen, braucht dafür einen Segen
· **5** mit dem Stein zu Azazel, dessen Ketten gelöst sind — **ihn wieder versiegeln.**

Zwei lateinische Formeln (Wortlaut in `doc/welt.md` §6): **„Satis est. Contine manum tuam."**
hält die Engel an, **„Quod fractum est, integrum fiat."** macht Zerbrochenes ganz.

Ton: düster, trist, Gothic 1. Ambivalenz statt klarer Fronten — jede Fraktion hat recht und blinde
Flecken. Magie kostet **Leben**, nicht Mana.

## Engine / Stack

**Godot 4.7**, .NET/C#, Forward+. Addons: **Terrain3D** (GDExtension), limboai, dialogue_manager.

## 🔧 Sprachregel: C# oder GDScript?

> **Spiel-Logik = C#.** GDScript nur für **(1)** fertige, shader-nahe Weltsysteme (`player.gd`,
> `day_night.gd`, `rock_collision.gd`, `wasser.gd`, Partikel) und **(2)** Addon-Kleber, der
> zwingend GDScript sein muss.

Grund: `dotnet build` ist ein Verifikationsschritt, den ein Agent selbst ausführen kann.
**Nach jeder C#-Änderung `dotnet build` laufen lassen** — muss 0 Fehler zeigen; die ~23 Warnungen
kommen alle aus `addons/` und sind normal.

**Der Player bleibt GDScript** (`scripts/World/player.gd`, fertig, eng mit Gras/Wasser/Kamera
verzahnt). Neue Logik hängt sich als C#-Kind-Node an. `scripts/Characters/Player.cs` ist **nicht**
der aktive Player. **Nie vorschlagen, das C#-Framework in GDScript neu zu schreiben.**

## Szenen

**Hauptszene `Main.tscn`** → `World/world.tscn` + `World/player.tscn` + `UI/Hud.tscn` + NPCs.

`world.tscn` enthält seit 30.07. **alles in einer Datei** (3265 Nodes): Terrain3D + Gras, Systeme,
Wasser, Blockout (CSG), Bauten, Orte, und `Bewuchs/` mit ~2400 handplatzierten Bäumen/Felsen/
Sträuchern in **28 Sektoren à 200 m**. Details in `doc/technik.md`.

## Inhalte = Daten, kein Code

Neuer NPC/Item/Quest → JSON unter `Data/`. Neuer Dialog → `.dialogue` unter `Dialogues/`. Dateien
mit `_` am Anfang ignoriert `GameData`. Dialoge laufen über das dialogue_manager-Addon, einziger
Einstieg `Dialog.Show(datei, titel, sprecher)`; was sie auslösen dürfen, steht in
`scripts/Dialogue/DialogueBridge.cs`.

**Ist-Stand:** 3 NPCs, 3 Dialoge, 4 Items, 1 Quest, 1 Zauber. Die früher genannten „30 NPCs,
27 Dialoge, 47 Items" wurden am 26.07. geleert — Inhalte müssen nach `doc/welt.md` neu geschrieben
werden. Währung ist **Silber**.

## Die wichtigsten Fallstricke

- **Der Player steht nur in `Main.tscn`.** Nicht wieder einen in `world.tscn` einbauen — es gab
  bis 30.07. einen zweiten, dadurch liefen zwei Kameras und zwei Körper gleichzeitig. Zum Testen
  über `Main.tscn` starten.
- **Nur ein Terrain3D-Node pro Szene.** Zwei auf dasselbe `data_directory` = Bemalen funktioniert
  nicht mehr.
- **Die Terrain-Asset-Liste liegt eingebettet in `world.tscn`**, nicht als Datei. Textur-Slots und
  Mesh-Assets über das Asset-Dock ändern.
- **`Bewuchs/rocks_umgebung` nicht auflösen** — der Knoten gibt seinen 18 Kindern per Script die
  Kollision.
- **Godot überschreibt `.tscn` beim Speichern** und lädt extern geänderte Scripts nicht neu,
  solange sie im Editor offen sind. Nach Datei-Edits neu laden lassen.
- **Blattschatten: `fade_margin = 0` lassen.** Godot-Bug mit Alpha-Clip, gelöst am 27.07. — die
  vergeblichen Versuche stehen in `doc/technik.md` §6, nicht wiederholen.
- **Bewuchs nie generieren lassen.** Die Generatoren (`place_vegetation.py`, `place_orte.py`,
  `ForestPainter.cs`) sind am 30.07. gelöscht worden, weil sie die Handarbeit in `world.tscn`
  überschreiben würden. Nicht aus Git zurückholen und laufen lassen.
- **Leistung ist Thema:** ~86 % GPU auf einer 2080 Ti, Ziel auch Laptops. Teuerste Posten: Gras,
  volumetrischer Nebel, OmniLights, Wasser, Schatten-/Baumdistanz, Draw Calls.

## Arbeitsweise

- **Commits und Push nur auf Anweisung.** Footer: `Co-Authored-By: Claude Opus 4.8 …`
- Lokaler Stand divergiert oft vom Remote (User rebast/reset lokal). Etablierter Weg nach
  Absprache: `git push --force-with-lease origin main`
- Für Performance immer FPS/ms im **laufenden Spiel** messen, nicht Editor-RAM.
