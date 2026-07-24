# Projekt aufräumen – Anleitung & Checkliste

> Erstellt als Leitfaden zum Selbst-Aufräumen. Reihenfolge von oben nach unten abarbeiten.
> Sicherheitsnetz: Alles ist gepusht – wenn etwas kaputtgeht, `git reset --hard origin/main`.

---

## 0. Die wichtigste Entscheidung zuerst: WELCHES Terrain ist dein Spiel?

Aktuell existieren **drei** Terrains parallel:

| Szene | Was es ist | Hängt an deinem Spiel? |
|---|---|---|
| `Maps/ground.tscn` (via `Level/World.tscn`) | **statisches GLB** `Models/Terrain_small.glb` + handplatzierte Bäume | **JA** – das lädt deine Hauptszene aktuell |
| `Objects/terrain.tscn` | **Terrain3D** + Gras + Wasser + Boden-Abdunklung (aus der Demo abgeleitet) | NEIN |
| `Models/terrain_3d.tscn` | altes Terrain3D | NEIN |
| `demo/*.tscn` | reine Demo-Szenen | NEIN |

**Deine ganze Gras-/Wasser-/Terrain-Arbeit liegt in `Objects/terrain.tscn`, nicht im Spiel.**

👉 **Entscheide:** Wird dein Spiel-Terrain das **Terrain3D** (dann muss `Objects/terrain.tscn` in `Level/World.tscn` wandern und `Maps/ground.tscn` + `Terrain_small.glb` raus)? Oder bleibst du beim statischen GLB (dann kann der ganze Terrain3D-/Demo-Kram weg)?

Alles Weitere hängt an dieser Entscheidung. Vermutlich willst du **Terrain3D** (dort steckt die Arbeit).

---

## 1. Grundregeln (damit keine Pfade brechen)

- **Verschieben/Umbenennen NUR im Godot-Editor** (FileSystem-Dock, per Drag & Drop oder Rechtsklick → Umbenennen). Godot zieht dann **Pfade UND uids** automatisch nach.
- **Niemals im Dateimanager/Terminal** verschieben – das bricht Referenzen.
- **Unbenutztes finden – der sichere Weg:** In Godot Rechtsklick auf eine Datei → **„Owners anzeigen…"**. Oder einfach **im Editor löschen versuchen**: Godot zeigt dann eine Warnung mit allen Abhängigkeiten. Kommt keine Warnung → nichts benutzt es → sicher.
- Nach jedem größeren Schritt: Projekt neu laden und unten die **Ausgabe/Fehler** prüfen.

---

## 2. Zielstruktur (Vorschlag)

```
res://
  game/                 # DEIN Spiel
    levels/             # World.tscn, Zones/
    characters/         # Player, Npc, Enemy
    objects/            # Items, Triggers, Props (Crate, Rock…)
    ui/                 # Hud
    data/               # JSON, Definitionen
  art/
    models/             # .glb / Baum-Szenen
    textures/
      library/          # Rohtexturen (noch ungenutzt) – VOM IMPORT AUSSCHLIESSEN, s. §4
      terrain/          # gepackte Terrain-Texturen (albedo+height, normal+roughness)
    materials/
    shaders/            # water.gdshader, terrain_shader.gdshader, grass shader…
  terrain/              # Terrain3D-Daten (terrain_data/) + Instancer/Assets
  scripts/              # C#
  addons/               # unverändert lassen
  doc/
```

> Die genaue Benennung ist Geschmack – wichtig ist: **Spiel** getrennt von **Rohmaterial (art)** getrennt von **Demo/Addons**.

---

## 3. Reihenfolge zum Aufräumen

1. **Branch anlegen** (optional, extra Sicherheit): `git switch -c aufraeumen`.
2. **Terrain-Entscheidung umsetzen** (§0):
   - Bei Umstieg auf Terrain3D: den Terrain3D-Node + `Terrain3DParticles` (Gras) + Wasser-Mesh aus `Objects/terrain.tscn` in `Level/World.tscn` (bzw. `Maps/ground.tscn`) übernehmen, dann `Terrain_small.glb` + alte Handplatzierung entfernen.
   - Prüfe am Terrain3D-Node: Eigenschaft **Data Directory** zeigt auf `res://terrain_data/` (dort liegen deine `terrain3d_*.res`).
3. **Alte/Demo-Terrains isolieren:** `Models/terrain_3d.tscn`, und – wenn Inhalt übernommen – `Objects/terrain.tscn`. Erst wenn nichts mehr darauf verweist (Owners prüfen!).
4. **`demo/`-Ordner:** Enthält die Terrain3D-Demo. Was brauchst du wirklich? Der angepasste Gras-Kram liegt in `addons/terrain_3d/extras/particle_example/` – den behalten. Die `demo/`-Szenen selbst brauchst du fürs Spiel nicht.
5. **Modelle prüfen:** In `Models/Various_Forest_Assets_Pack/` – welche Bäume nutzt du (Instancer-Assets in `demo/data/assets.tres`)? Ungenutzte in die Library.
6. **Texturen ordnen** (§4).
7. **Ordnerstruktur** (§2) im Editor herstellen – Ordner für Ordner, immer im FileSystem-Dock.
8. **Testlauf** (F5) + Ausgabe prüfen.
9. Commit in sinnvollen Häppchen.

---

## 4. Texturen: behalten, aber verschlanken

Du willst alle behalten, brauchst sie aber nicht in dem Ausmaß. Drei Hebel:

**a) Ungenutzte Rohtexturen als Library auslagern & vom Import ausschließen**
- Verschiebe (im Editor!) die noch ungenutzten Sets nach `art/textures/library/`.
- Lege dort eine leere Datei **`.gdignore`** an. Dann **ignoriert Godot den Ordner komplett**: keine `.import`-Dateien, kein Platz im `.godot/imported`-Cache, kein Editor-Ballast. Die Dateien bleiben aber im Projekt/Repo erhalten.
- Wenn du eine Textur nutzen willst: aus `library/` in einen importierten Ordner ziehen.

**b) Nicht benötigte Kanäle löschen**
- Die AmbientCG-Sets haben pro Material oft 6–8 Dateien (Color, NormalGL **und** NormalDX, Roughness, Displacement, AO, Metalness…). Für PBR brauchst du meist nur **Color + Normal(GL) + Roughness (+ AO)**. Displacement/Metalness/den zweiten Normal-Typ kannst du löschen → grob halbiert die Größe.

**c) Erst beim Verwenden „packen"**
- Fürs Terrain3D: Color+Height → eine Datei, Normal+Roughness → eine Datei. Das macht am einfachsten das eingebaute **Terrain3D → „Pack Textures"** (Menü oben, wenn der Terrain3D-Node gewählt ist). Nicht auf Vorrat für alle 29 Sets – nur für die, die du tatsächlich einsetzt.

**d) Optional: Git LFS**
- `Textures/` und `Models/` sind zusammen ~650 MB im Repo. Mit **Git LFS** (`git lfs track "*.png" "*.jpg" "*.glb"`) bleibt das Repo schlank. Einmalig einzurichten; sag Bescheid, dann mache ich das.

---

## 5. Was du dir konkret ansehen solltest

- [ ] `project.godot` → `run/main_scene` = `Level/World.tscn` (deine echte Hauptszene) – stimmt das noch?
- [ ] `Level/World.tscn`: lädt `Maps/ground.tscn` (statisches Terrain) + `Maps/Environment.tscn` + Player + Zonen + UI.
- [ ] `Maps/ground.tscn`: statisches `Terrain_small.glb` + handplatzierte Bäume – **hier** entscheidet sich, ob Terrain3D rein soll.
- [ ] `Objects/terrain.tscn`: hier liegt deine Gras/Wasser/Terrain3D-Arbeit – **übernehmen oder verwerfen?**
- [ ] `Models/terrain_3d.tscn`: alt – vermutlich weg.
- [ ] `terrain_data/`: die 4 `terrain3d_*.res` – zu welchem Terrain3D-Node gehören die? Der behaltene Terrain-Node muss darauf zeigen.
- [ ] `demo/`: Demo-Szenen – fürs Spiel nicht nötig.
- [ ] Doppelte Texturen im Projektwurzel: `packed_albedo_height.png` & `Textures/packed_albedo_height.png` (es gibt beide!) – eine ist überflüssig.
- [ ] Shader/Materialien, die du behalten willst, an einen festen Ort (`art/shaders/`): `water.gdshader`, `terrain_shader.gdshader`, `grass_noise.tres`, Gras-Shader unter `addons/.../particle_example/`.

---

## 6. Reihenfolge beim Löschen (sicher)

1. Kandidat im FileSystem-Dock anklicken → **Owners anzeigen**.
2. Keine Owner? → Löschen im Editor.
3. Godot zeigt beim Löschen ggf. betroffene Dateien – wenn Liste leer, gefahrlos.
4. Zwischendurch F5-Testlauf.

> Wenn du unsicher bist: statt löschen erst in `art/textures/library/` (mit `.gdignore`) schieben – dann ist es „weg", aber wiederholbar.

---

## Kurz-Zusammenfassung
1. **Zuerst die Terrain-Frage klären** (§0) – das ist die eigentliche Unordnung.
2. Alles **im Editor** verschieben, nie im Terminal.
3. Rohtexturen in `library/` + `.gdignore` → behalten, aber schlank.
4. Zum Prüfen: **„Owners anzeigen"** bzw. Lösch-Warnung des Editors.
