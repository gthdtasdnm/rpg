# Weltausstattung – Bewuchs und Orte (2026-07-28)

Erste regelbasierte Ausstattung der Terrain-Welt: Felsen, Bäume, Sträucher und
Blocking-Boxen für Orte. Alles aus den Terrain3D-Höhendaten abgeleitet, nicht geraten.

## Was neu ist

| Datei | Inhalt |
|---|---|
| `World/bewuchs.tscn` | 1674 Knoten: 421 Felsen, 656 Bäume, 520 Sträucher, 13 Totholz-Landmarken |
| `World/orte.tscn` | 116 CSG-Boxen in 8 Ortsgruppen |
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

**Bäume** stehen in 95 Gruppen mit gaußförmiger Streuung um einen Mittelpunkt, plus
1–3 Ausläufern pro Gruppe, damit die Gruppe keine harte Kante hat. Artenwahl nach
Höhenlage: über 38 m Nadelholz, 18–38 m gemischt, darunter Laub und Birke. 18 %
Jungbäume. Mindestabstand 4,2 m, Skalierung 0,78–1,32.

**Straßenrahmung** – der wichtigste Block, läuft zuerst und hat Vorrang vor der
allgemeinen Streuung. Entlang der Straßen-Mittellinie alle 26–42 m eine Gruppe,
**abwechselnd links und rechts**. Das bricht die Symmetrie des Tals und füllt den
Mittelgrund, den eine reine Zufallsstreuung nicht trifft. 63 Objekte an 14 Stellen.

**Freigehalten** werden: die Straße selbst, das Flussbett, Wasserflächen, Nethora
samt Vorfeld (x −710…−520, z −690…−450), ein 7,5-m-Ring um jedes von Hand gesetzte
Objekt und der Umkreis jedes Ortes.

## Die acht Orte

Positionen aus einer Geländesuche: ebene Fläche, passende Nachbarschaft (Wand,
Wasser, Straße), Mindestabstand zu Nethora.

| Ort | Position | Lage | Rolle |
|---|---|---|---|
| Wegschrein Ankunft | −676, −917 | direkt hinter dem Tor | erstes „hier war jemand" nach der Ankunft |
| Rastplatz | −364, −807 | an der Ostroute | Feuerstelle, Bänke, Unterstand |
| Fährstelle | −431, −787 | Flussufer | Hütte + Steg, für Fährmann Aldous |
| Freies Lager | −605, −165 | Felswand im Süden | Höhlenmaul, 6 Hütten, Palisade, Wachturm |
| Alte Kathedrale | −592, −269 | Ebene südlich Nethora | Ruine, Langhaus + Turmstumpf |
| Erbauer-Tempel | −106, −399 | Plateau auf 50 m | gestufte Plattform, 4 Monolithen |
| Mine von Nethora | −862, −306 | Westgebirge | Stollenmund, Fördergerüst, Halden |
| Verlassenes Gehöft | −312, −626 | Südroute | Haupthaus, Nebengebäude, Zaun |

Das Freie Lager saß zunächst auf (−727, −142) – dort steigt das Gelände im 25-m-Umkreis
nur 2,6 m an, ein Höhlenmaul hätte frei in der Landschaft gestanden. Der jetzige Platz
hat 18 % Steilfläche im Umkreis und 13 m Anstieg.

## Kontrolliert

- Godot lädt das Projekt headless fehlerfrei (`--headless --quit-after`)
- 0 Objekte auf der Straße
- 2 Objekte im Nethora-Bereich (Randlage, unkritisch)
- kleinster Abstand zu bestehenden Handplatzierungen: 4,4 m
- Höhenversatz −4,3 m (eingesenkte Großfelsen) bis +1,1 m

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
python3 scripts/Tools/place_vegetation.py   # -> World/bewuchs.tscn
python3 scripts/Tools/place_orte.py         # -> World/orte.tscn
```

Beide Skripte überschreiben ihre Zieldatei vollständig. Wer einzelne Objekte im Editor
verschiebt, verliert das beim nächsten Lauf – dann besser die Szene umbenennen und
aus der Generierung nehmen. Der Zufallsstartwert steht oben in den Skripten
(`default_rng(20260728)`); ihn zu ändern erzeugt eine komplett andere Verteilung.

Die Skripte erwarten die entpackten Karten als `.npy` im Scratchpad-Pfad, der oben in
der Datei steht. Bei Bedarf über `terrain3d_maps.py` neu erzeugen – nötig, sobald am
Terrain gesculptet wurde.
