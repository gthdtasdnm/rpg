# Blattschatten reichen nicht weit genug — GELÖST am 27.07.2026

## 🔑 Die Ursache: `fade_margin` + Alpha-Clip = Godot-Fehler

`fade_margin > 0` lässt Terrain3D `visibility_range_fade_mode = SELF` setzen. In Godot 4 zerstört
das die Schatten von Materialien mit **Alpha Clip / Alpha Hash** — und genau das sind unsere
Blattmaterialien (`transparency = 2`, `alpha_scissor_threshold`) und der Imposter-Shader
(`ALPHA_SCISSOR_THRESHOLD`, `depth_prepass_alpha`).

- [godot#91671 — Visibility Range „Fade Self" hides shadows unexpectedly in Depth Pre-Pass](https://github.com/godotengine/godot/issues/91671)
- [godot#88854 — Visibility Range Fade does not work correctly with Alpha Clip and Alpha Hash Materials](https://github.com/godotengine/godot/issues/88854)

`fade_margin` stand von Anfang an auf 5. **Deshalb** reichten die Blattschatten nur ~30 m,
während die Stammschatten (Material ohne Alpha, also nicht betroffen) weiter reichten. Es war
nie eine falsch eingestellte Distanz.

**Workaround, der gilt: `fade_margin = 0` bei allen Baum-Assets.** Nicht wieder hochsetzen —
A/B nachgewiesen: `fade_margin = 0` → durchgehender Schatten + hartes Popping,
`fade_margin = 6` → weicher Übergang + Schattenlücke. Es gibt bei diesem Engine-Fehler kein
Beides.

**Offen bleibt nur noch:** ein weicher LOD-Übergang ohne Godots kaputte Fade-Funktion, siehe
Abschnitt „Weicher Übergang" unten.

---

Stand vor der Lösung (Verlauf, zur Nachvollziehbarkeit):

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

## 🎯 Festgelegte Priorität: Popping schlägt Schatten

Nach zwei Tagen A/B-Tests hat der User entschieden: **wenn zwischen weichem Übergang und
Schattenreichweite gewählt werden muss, gewinnt der weiche Übergang.** Daraus folgt die
Einstellung in allen 23 Baum-Assets (`World/world.tscn`):

```
last_lod = 1
last_shadow_lod = 0     ; NUR der echte Baum wirft Schatten, der Imposter nicht
lod0_range = 140..160   ; echter Baum, pro Baumart leicht anders (CRC32 aus dem Namen)
lod1_range = 600.0      ; Imposter
fade_margin = 40.0      ; langer Übergang, weit weg
```
`World/Environment.tscn`: `directional_shadow_max_distance = 200.0`.
`project.godot`: `lights_and_shadows/directional_shadow/size=8192`.

Begründung der Werte:

- **`fade_margin = 40`** — langer Überblendbereich, damit das Popping verschwindet. Der
  Godot-Fehler (ganz oben) verkürzt dafür die Blattschatten; das ist die bewusst akzeptierte
  Gegenleistung. Terrain3D kappt `fade_margin` bei 64 m bzw. der halben LOD0–LOD1-Distanz.
- **`lod0_range = 140…160`** — der Wechsel soll weit weg passieren, und die echten Bäume sollen
  ihre Schatten so weit wie möglich mitnehmen. ⚠️ **Das ist der teuerste Wert im ganzen Wald**:
  volle Meshes (5.000–26.000 Dreiecke, ~2.000 Instanzen) bis 150 m statt bis 80 m. Bei
  FPS-Problemen ist das die erste Zahl, die runter geht.
- **`last_shadow_lod = 0`** — Imposter-Schatten sind ausdrücklich nicht gewünscht.

## Wenn die Lücke bleibt: messen statt probieren

`scripts/Tools/debug_tree_lod.gd` im Editor ausführen (Strg+Umschalt+X). Der Abschnitt
**„Schatten pro Sorte"** zeigt, was der Instancer tatsächlich erzeugt hat. Erwartet werden genau
zwei Sorten Knoten:

```
  viele Dreiecke  begin=0     end=30    cast_shadow=an
       4 Dreiecke begin=30    end=600   cast_shadow=an
```

Weicht das ab — `cast_shadow=AUS` bei einer Sorte, oder eine Lücke zwischen `end` der einen und
`begin` der anderen — ist das die Ursache, schwarz auf weiß. Vorher keine weiteren
Kombinationen ausprobieren.

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

## Weicher Übergang — der einzige verbleibende Weg

Godots eingebaute Überblendung ist für uns unbrauchbar (Fehler oben). Ein weicher Übergang geht
nur noch selbst gebaut, und zwar so:

1. Die Sichtbereiche der MMIs nach dem Aufbau durch Terrain3D **überlappend** setzen
   (`visibility_range_begin/end` direkt am Knoten, `fade_mode = DISABLED`) — ein Script, das
   den MMI-Baum durchläuft. Über die Terrain3D-Einstellungen geht es nicht: dort erzeugt nur
   `fade_margin` eine Überlappung, und das schaltet zwangsläufig `SELF` mit ein.
2. Das Ausblenden **in den eigenen Shadern** per Dither-Rauschen über die Kameradistanz. Der
   Imposter-Shader hat den Zufallswert pro Baum bereits (`imposter.gdshader`) — damit wäre die
   gewünschte Streuung *pro einzelnem Baum* möglich, die über Terrain3D-Einstellungen nicht geht.
3. Dafür müssen die 23 Baum-Materialsätze (Rinde + Blatt, aktuell `StandardMaterial3D`) auf
   Shader umgestellt werden, sonst lässt sich LOD0 nicht ausblenden.

Aufwand: der teure Teil ist Punkt 3. Vorher abwägen, ob das Popping bei 70–90 m überhaupt stört.
