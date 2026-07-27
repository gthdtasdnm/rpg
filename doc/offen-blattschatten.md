# Offenes Problem: Blattschatten reichen nicht weit genug

Stand: 27.07.2026, ungelöst. Übergabe an die nächste Sitzung.

## Das Problem (Beschreibung des Users, unverändert)

> Ich möchte Schatten, die ca. 100 Meter reichen. Aber die Schatten der Blätter werden nur auf
> 20 Meter angezeigt. Stattdessen wird der Schatten der Baumstümpfe angezeigt.

Beobachtet außerdem: Nähert man sich einem Baum, erscheint der Blattschatten ab ca. 20–30 m
zusätzlich zum Stammschatten. Entfernt man sich, verschwindet er wieder.

**Nicht weiter interpretieren** — die bisherigen Erklärungsversuche haben nicht zur Lösung
geführt und mehrfach neue Probleme erzeugt.

## Was bereits versucht wurde (alles ohne Erfolg)

| Versuch | Wert | Ergebnis |
|---|---|---|
| `alpha_scissor_threshold` der Blattmaterialien senken | 0.5 → 0.33 → 0.2 | keine sichtbare Änderung am Blattschatten |
| `directional_shadow_max_distance` erhöhen | 130 → 320 | Blattschatten wurden **schlechter**, auch im Nahbereich; zurückgesetzt |
| `directional_shadow_max_distance` senken | 130 → 90/100 | keine Verbesserung des Blattschattens |
| Schattenkarte vergrößern | 4096 → 8192 (`project.godot`) | keine sichtbare Änderung; Einstellung ist noch aktiv |
| `shadow_blur` senken | 2.0 → 0.5 | zuletzt gesetzt, Ergebnis vom User **nicht mehr bestätigt** |
| MSAA + Alpha-to-Coverage | 4x, `alpha_antialiasing_mode = 1` | weiße Kreise/Flackern auf dem Bildschirm; zurückgesetzt |
| `distance_fade_mode = 3` (Dithering) an Blattmaterialien | — | Blätter verblassten beim Kameraschwenk; zurückgesetzt |
| Blob-Schatten unter den Impostern (dunkler Fleck am Boden) | — | funktionierte sichtbar, kollidierte aber mit der LOD-Überblendung (echter Schatten + Blob gleichzeitig sichtbar); wieder entfernt, Code im Script über `SHADOW_STRENGTH = 0.0` deaktiviert |

## Aktueller Zustand der relevanten Einstellungen

**`World/Environment.tscn`**, Knoten `DirectionalLight3D`:
```
shadow_enabled = true
shadow_blur = 0.5
directional_shadow_blend_splits = true
directional_shadow_max_distance = 130.0
```

**`project.godot`**:
```
lights_and_shadows/directional_shadow/size=8192
```

**`Assets/Nature/Forest/merged/*.tscn`** (23 Bäume), Blattmaterial:
```
transparency = 2              (ALPHA_SCISSOR)
alpha_scissor_threshold = 0.2
alpha_antialiasing_mode = 0
cull_mode = 2                 (doppelseitig)
```
Der Stamm nutzt ein zweites Material **ohne** Alpha. Genau dieser Stammschatten ist der, den man
in der Ferne noch sieht.

**Baum-LODs**, eingebettet in `World/world.tscn` (siehe Warnung unten):
```
last_lod = 1
last_shadow_lod = 0
fade_margin = 20.0
lod0_range = 65.0      ; volles Mesh
lod1_range = 1200.0    ; Imposter
```
**Überholt seit 27.07. (siehe unten):** `last_shadow_lod` steht jetzt bei allen 23 Baum-Assets
auf `1`, und die `cast_shadow`-Zeile an den `LOD1`-Knoten ist entfernt (Standard = Schatten an).

## Änderung 27.07.: Imposter werfen Schatten

Der Imposter ist kein zur Kamera gedrehtes Billboard mehr, sondern ein **festes Kreuz aus zwei
Ebenen mit zufälliger Drehung** pro Baum. Damit fällt der Grund weg, warum er keinen Schatten
werfen durfte (ein mitdrehender Schatten fällt sofort auf).

Der eigentliche Deckel für die Schattenreichweite der Bäume war **nicht** eine Distanz, sondern
`last_shadow_lod = 0` am `Terrain3DMeshAsset`: nur LOD0 durfte Schatten werfen, und LOD0 endet
bei `lod0_range = 95 m`. Terrain3D hat keinen eigenen Zahlenwert für Schattenreichweite — sie
ergibt sich aus *welche* LODs werfen dürfen mal *deren* Range.

Jetzt: `last_shadow_lod = 1` → LOD1 (Imposter, `lod1_range = 1200 m`) wirft mit. Damit ist die
Terrain3D-Seite ausgereizt; begrenzend ist ab jetzt nur noch
`directional_shadow_max_distance = 160` in `World/Environment.tscn`.

**Danach beobachtet:** Schatten von 0–30 m, dann eine große Lücke, ab ~65 m wieder Schatten
(die Imposter). Die Lücke ist der alte, unerklärte 30-m-Effekt — er war vorher nur nicht als
Lücke sichtbar, weil dahinter überhaupt nichts mehr kam.

Gegenmittel: **`shadow_impostor = 1`** (ebenfalls bei allen 23 Baum-Assets gesetzt). Laut
Terrain3D-Doku: *„Uses this lower quality LOD to calculate shadows (as an impostor) instead of
the visible mesh."* Damit wirft **immer** das Imposter-Kreuz den Schatten, auch für die nahen
echten Bäume — ein einziger Schattenwerfer über die ganze Distanz, also kein Umschaltpunkt und
keine Lücke. Preis: nah sieht man die Kreuz-Silhouette statt echter Blattschatten.

## Regler zum Ausprobieren

`scripts/World/ShadowTuner.cs`, hängt als Knoten `ShadowTuner` in `World/world.tscn`. Im
Inspector live verstellbar (Editor und Spiel), schreibt direkt auf Licht und alle Baum-Assets:

| Regler | schreibt auf |
|---|---|
| `Shadow Distance` | `directional_shadow_max_distance` am `DirectionalLight3D` |
| `Tree Lod0 Range` | `lod0_range` aller Baum-Assets (Umschaltpunkt echter Baum → Kreuz) |
| `Imposter Casts Shadows` | `last_shadow_lod` (1/0) |
| `Shadows From Imposter Only` | `shadow_impostor` (1/0) |

Assets mit `last_lod = 0` (Steine, Stümpfe, Gras-Karte) werden übersprungen — dort wäre
`last_shadow_lod > last_lod` ungültig. Gefundene Werte bleiben nur erhalten, wenn `world.tscn`
gespeichert wird.

## ⚠️ Fallstricke, die Zeit gekostet haben

1. **`World/data/assets.tres` wird NICHT gelesen.** Die Terrain3D-Asset-Liste liegt eingebettet
   in `World/world.tscn` (`assets = SubResource(...)`). `assets.tres` ist eine verwaiste Kopie;
   Änderungen dort bleiben wirkungslos.
2. **Godot lädt extern geänderte Scripts nicht automatisch neu.** Ist eine Datei im
   Script-Editor geöffnet, wird beim Ausführen die alte Fassung benutzt. Tab schließen und neu
   öffnen, sonst testet man stundenlang eine Version, die es nicht mehr gibt.
3. **Godot schreibt beim Speichern Werte in `project.godot` zurück** (z.B. tauchte ein längst
   gelöschter Autoload `DialogueRunner` mehrfach wieder auf).
4. **Terrain3D verwirft die Position der LOD-Knoten** — eine Höhenverschiebung muss über
   `PrimitiveMesh.center_offset` ins Mesh selbst.
5. **`mesh.get_aabb()` ist bei den zusammengefügten Bäumen unbrauchbar** (meldet Mitte 0,0,0).

## Das Imposter-System (funktioniert, nicht anfassen)

Erzeugt von `scripts/Tools/generate_tree_imposters.gd` (im Script-Editor öffnen, Strg+Umschalt+X):
- rendert je Baum 4 Ansichten in einen Atlas (`Assets/Nature/Forest/imposters/*.png`)
- baut daraus einen `LOD1`-Knoten in jede Baumszene: **zwei überkreuzte Ebenen** (von oben ein X)
- `imposter.gdshader` dreht jeden Baum fest und zufällig um die Hochachse (kein Billboard mehr)
  und streut Ansicht, Farbton, Helligkeit und Rauschen pro Baum anhand der Weltposition

Ergebnis: Baum-Dreiecke von 9,3 Mio auf ~540.000, Sichtweite 500 m → 1200 m. Der User hat das
Aussehen als „perfekt" bewertet — **einziger offener Punkt ist der Blattschatten**.

## Wo die Leistung aktuell steht

Gemessen mit dem Overlay (F3; F4 V-Sync, F5 Gras, F6 Bäume):

| Zustand | FPS | Dreiecke |
|---|---|---|
| ohne Gras, ohne Bäume | 300 | 560.000 |
| nur Bäume | 270 | 1.100.000 |
| nur Gras | 160 | 5.200.000 |
| beides | ~150 | ~5.700.000 |

**Nächstes Optimierungsthema wäre das Gras** (kostet ~8× so viel wie die Bäume). Idee des Users:
gebündelte Büschel-Meshes statt einzelner Halme — weniger Instanzen bei gleicher Dichte.

Außerdem offen: gelegentlicher Hänger von ~145 ms (`min 1 FPS` im Overlay).
