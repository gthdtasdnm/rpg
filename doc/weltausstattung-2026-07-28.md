# Weltausstattung – Bewuchs und Orte (2026-07-28)

Erste regelbasierte Ausstattung der Terrain-Welt: Felsen, Bäume, Sträucher und
Blocking-Boxen für Orte. Alles aus den Terrain3D-Höhendaten abgeleitet, nicht geraten.

## Was neu ist

| Datei | Inhalt |
|---|---|
| `World/bewuchs.tscn` | 7911 Knoten: 2161 Felsen, 3095 Bäume, 2655 Sträucher, 23 Totholz-Landmarken |
| `World/orte.tscn` | 117 CSG-Boxen in 10 Gruppen – **je Ort genau eine Box als Zeichen**, kein Gebäudenachbau |
| `scripts/Tools/terrain3d_read.py` | entpackt Terrain3D-`.res` (ZSTD-Blöcke) |
| `scripts/Tools/terrain3d_maps.py` | liest Höhe / Control / Farbe als NumPy-Arrays |
| `scripts/Tools/place_vegetation.py` | erzeugt `bewuchs.tscn` |
| `scripts/Tools/place_orte.py` | erzeugt `orte.tscn` |

Beide Szenen hängen in `World/world.tscn` als Instanz (`Bewuchs`, `Orte`). Wer sie
loswerden will, löscht die zwei Knoten – am Rest der Szene ändert sich nichts.

## Terrain3D-Daten von außen lesen

Die Regionsdateien in `World/data/` sind **ZSTD-blockkomprimierte Godot-Ressourcen**
(Magic `RSCC`, Blockgröße 4096). Entpackt enthält jede Region drei 1024×1024-Bilder:
Höhe (`RFloat`), Control (`uint32`) und Farbe (`RGBA8`, mit Mipmaps).

Damit lässt sich außerhalb von Godot auswerten: Höhe an jeder Koordinate,
Hangneigung, bemalte Texturen. Genau darauf beruht die Platzierung.

**Weltmaße:** 2048 × 2048 m, vier Regionen, `vertex_spacing = 1`.
Höhen von −14,9 m bis 170,7 m. 7,3 % der Fläche sind steiler als 45° und damit
für den Spieler Wand (`floor_max_angle` steht auf Godots Standard).

**Texturbelegung** (bestätigt vom Projektinhaber):
- Textur 2 = Straßen
- Textur 3 = Flussbett

## Platzierungsregeln

**Felsen** liegen dort, wo die Neigung zwischen 12° und 42° ist *und* im 15-m-Umkreis
eine echte Wand (>48°) steht – also am Fuß von Steilhängen, wo Geröll natürlich
liegen bleibt. Jede Fundstelle bekommt eine Gruppe: ein großer Brocken, 2–4 mittlere,
3–6 kleine. Alle 20–42 % ins Gelände eingesenkt und leicht gekippt.

**Bäume** stehen in 216 Gruppen mit gaußförmiger Streuung um einen Mittelpunkt, plus
1–3 Ausläufern pro Gruppe, damit die Gruppe keine harte Kante hat. Artenwahl nach
Höhenlage: über 38 m Nadelholz, 18–38 m gemischt, darunter Laub und Birke. 18 %
Jungbäume. Mindestabstand 2,9 m, Skalierung 0,78–1,32.

**Straßenrahmung** – der wichtigste Block, läuft zuerst und hat Vorrang vor der
allgemeinen Streuung. Entlang der Straßen-Mittellinie alle 16–28 m eine Gruppe,
**abwechselnd links und rechts**. Das bricht die Symmetrie des Tals und füllt den
Mittelgrund, den eine reine Zufallsstreuung nicht trifft. 186 Objekte an 21 Stellen.

**Freigehalten** werden: die Straße selbst, das Flussbett, Wasserflächen, Nethora
samt Vorfeld (x −710…−520, z −690…−450), ein 7,5-m-Ring um jedes von Hand gesetzte
Objekt und der Umkreis jedes Ortes.

## Die Orte – eine Box je Ort

Jede Box ist **nur ein Zeichen**: „hier steht später etwas, ungefähr so groß." Kein
Nachbau von Gebäuden, das ist Handarbeit für später. Die Box steckt zu ~18 % im Boden,
damit sie am Hang nicht schwebt.

**Feste Orte aus der Lore** (`doc/konzept/`):

| Ort | Position | Maße | Rolle |
|---|---|---|---|
| Freies Lager | −605, −165 | 70×14×70 | Berghöhlengewölbe der Freien |
| Alte Kathedrale | −592, −269 | 36×22×18 | Ruine aus der Zeit vor der Barriere |
| Erbauer-Tempel Ost | −106, −399 | 42×26×42 | Plateau auf 50 m |
| Mine von Nethora | −862, −306 | 24×16×24 | Westgebirge |
| Verlassenes Gehöft | −312, −626 | 28×10×24 | Südroute |
| Fährstelle Aldous | −431, −787 | 12×7×10 | Flussquerung |
| Rastplatz Ostroute | −364, −807 | 10×4×10 | Lagerfeuer |
| Wegschrein Ankunft | −676, −917 | 4×6×4 | erster Schrein hinterm Tor |

**Pentagramm:** Haupttempel bei Nethora + 4 äußere, auf einem Kreis von 470 m um den
Kesselmittelpunkt, jeder auf den nächsten tauglichen Bauplatz gesetzt.

**Automatisch gefunden**, nach Geländekriterien:

| Gruppe | Anzahl | Kriterium |
|---|---|---|
| Ruinen | 30 | eben, abseits der Straße, >170 m von Nethora |
| Lagerplätze | 30 | eben, >150 m von Nethora |
| Höhlen | 24 | ebener Vorplatz mit Felswand im Umkreis |
| Wachtürme | 7 | lokale Erhebung über dem Umgebungsmittel |
| Furten | 6 | Flussbett-Ränder |
| Wegschreine | 4 | direkt an der Straße |
| Kultplätze | 2 | eben, abgelegen, ohne Wand |
| Flussquerung | 1 | Straße kreuzt Flussbett |

**Startkorridor und Nethora sind gesperrt** (150 m um den Spielerstart, Stadtrechteck).
Ein Pentagramm-Tempel landete zunächst 67 m vom Start – ein 34-m-Klotz mitten in der
Ankunftsszene. Beide Skripte halten diese Zonen jetzt frei.

Die Ortspositionen wandern über `orte_positions.json` an `place_vegetation.py`, damit
kein Baum durch eine Ruine wächst. **Deshalb `place_orte.py` immer zuerst ausführen.**

## Kontrolliert

- Godot lädt das Projekt headless fehlerfrei (`--headless --quit-after`)
- 0 Objekte auf der Straße
- 22 Objekte am Rand des Nethora-Rechtecks (Randlage, unkritisch)
- 103 Objekte im 100-m-Umkreis des Spielerstarts als Material zum Umstellen
- `World/bewuchs.tscn` ist 1,5 MB groß – der Editor kommt damit klar, aber die Datei
  ist nichts mehr, was man von Hand aufmacht

## Was ausdrücklich offen bleibt

**Die Eröffnungsansicht ist nicht gestaltet.** Regeln erzeugen Grundlast und Rhythmus,
aber die Rahmung des ersten Blicks – welcher Fels wo angeschnitten im Bild steht,
wann Nethora sichtbar wird – entscheidet sich aus einer Kameraposition auf Augenhöhe.
Das ist Handarbeit. Die Startgegend hat jetzt 34 Objekte im 80-m-Umkreis als
Ausgangsmaterial zum Umstellen, nicht als fertiges Bild.

**Maßstabsfrage weiter offen:** `player.gd` hat `speed = 12.0` (720 m/min). Das Tal
ist auf Spielerhöhe rund 180 m breit, Gothics Schluchten liegen bei 40–60 m. Entweder
Tempo runter oder Enge erhöhen – die Entscheidung wirkt sich auf jede spätere
Platzierung aus.

**Nicht geprüft:** wie es tatsächlich aussieht. Die Platzierung beruht auf Höhen- und
Neigungsdaten, nicht auf einem gerenderten Bild.

## Erneut erzeugen

```bash
python3 scripts/Tools/place_orte.py         # -> World/orte.tscn  (ZUERST)
python3 scripts/Tools/place_vegetation.py   # -> World/bewuchs.tscn
```

Die Reihenfolge zählt: `place_orte.py` schreibt die Ortspositionen, die
`place_vegetation.py` als Freihaltezonen liest.

Beide Skripte überschreiben ihre Zieldatei vollständig. Wer einzelne Objekte im Editor
verschiebt, verliert das beim nächsten Lauf – dann besser die Szene umbenennen und
aus der Generierung nehmen. Der Zufallsstartwert steht oben in den Skripten
(`default_rng(20260728)`); ihn zu ändern erzeugt eine komplett andere Verteilung.

Die Skripte erwarten die entpackten Karten als `.npy` im Scratchpad-Pfad, der oben in
der Datei steht. Bei Bedarf über `terrain3d_maps.py` neu erzeugen – nötig, sobald am
Terrain gesculptet wurde.
