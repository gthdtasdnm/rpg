# Assets/ — alles Sichtbare

Modelle, Texturen und Materialien. **Ein Ordner pro Sache, nicht pro Dateityp** — Modell und
zugehörige Texturen bleiben zusammen (siehe unten, „Warum nicht nach Dateityp").

```
Assets/
  Nature/       Forest/ (Bäume, Sträucher) · Rocks/ (Steine)     ← in Benutzung
  Terrain/      Packed/ (BPTC-gepackt) · Source/ (Roh-Texturen)  ← in Benutzung
  Buildings/    MedievalVillage/     176 modulare Gebäudeteile
  Props/        FantasyProps/ (94) · Tavern/ (84 Einrichtung)
  Weapons/      KickinIt/ (36, texturiert) · LowPolyMedieval/ (62)
  Characters/   ModularOutfits/      24 Outfits (Peasant, Ranger …)
  _Blender/     RetroMedievalKit/    nur .blend, muss erst exportiert werden
```

Jedes gekaufte Paket hat innen die gleiche Aufteilung: `Models/` + `Textures/`, daneben die
Lizenz- und Readme-Dateien des Anbieters.

## Herkunft und Lizenz

| Ordner | Originalpaket |
|---|---|
| `Buildings/MedievalVillage/` | Medieval Village MegaKit [Standard] |
| `Props/FantasyProps/` | Fantasy Props MegaKit [Standard] |
| `Props/Tavern/` | soiTavern_fbx |
| `Weapons/KickinIt/` | Low Poly Weapon Pack **with Image Texture** – Kickin It Studios |
| `Weapons/LowPolyMedieval/` | Low Poly Medieval Weapons |
| `Characters/ModularOutfits/` | Modular Character Outfits – Fantasy [Standard] |
| `_Blender/RetroMedievalKit/` | 3D Retro Medieval Fantasy Kit |
| `Nature/Forest/` | Various Forest Assets Pack (Bäume in `merged/` selbst zusammengeführt) |

**Lizenzdateien liegen in den Paketordnern und bleiben dort.** Mehrere verlangen Namensnennung —
vor einer Veröffentlichung durchsehen.

⚠️ **Die gekauften Pakete sind nicht im Git-Repo** (siehe `.gitignore`) — zu groß für die
Historie. `Nature/` und `Terrain/` dagegen schon, weil sie bearbeitet und nicht reproduzierbar
sind. Auf einem neuen Rechner: Pakete erneut herunterladen und nach obiger Tabelle einsortieren.

## Warum nicht nach Dateityp (`Models/`, `Textures/` global)?

Weil zusammengehörige Dinge sonst auseinanderfallen. Ein glTF verweist **relativ** auf seine
Texturen — liegen sie in einem anderen Ordner, bricht das beim Verschieben. Und beim Aufräumen
sieht man nicht mehr, welche Textur noch gebraucht wird.

Godot-Projekte werden deshalb **nach Domäne** sortiert, nicht nach Dateiendung. Das ist auch die
Antwort auf „Model/View/Controller": **MVC passt hier nicht.** Das ist ein Muster für
Anwendungscode, nicht für Spiel-Assets. Die Trennung im Projekt läuft anders:

| Ordner | Enthält |
|---|---|
| `Assets/` | Sichtbares: Modelle, Texturen (dieser Ordner) |
| `Data/` | Spieldaten als JSON: Charakterwerte, Items, Quests |
| `Dialogues/` | Gesprächstexte (`.dialogue`) |
| `Objects/` | Wiederverwendbare Spielobjekte: Wasser, Trigger, Checkpoint |
| `World/` | Die Weltszene selbst, Shader, Umgebung, Spieler |
| `UI/` | Bildschirmoberflächen: HUD, Dialogbox |
| `scripts/` | Der gesamte Code, nach Verantwortung (Characters, Quests, Dialogue …) |

Das ist bereits eine Trennung von Daten (`Data/`, `Dialogues/`), Darstellung (`Assets/`, `UI/`)
und Logik (`scripts/`) — nur eben mit Namen, die zum Spiel passen.

## Assets verschieben — wichtig

Godot merkt sich zu jeder importierten Datei eine **UID** in der zugehörigen `.import`-Datei.
Szenen verweisen über diese UID *und* über den Pfad.

- **Im Godot-Editor verschieben** (FileSystem-Dock, Drag & Drop): Godot zieht alle Verweise
  automatisch nach. **Das ist der sichere Weg.**
- **Im Dateimanager/Terminal verschieben**: die `.import`-Datei **muss** mitwandern, sonst
  verliert die Datei ihre UID und alle Verweise darauf brechen. Zusätzlich müssen die Pfade in
  `.tscn`/`.tres` von Hand korrigiert werden.

Beim Umbau am 26.07. wurde der zweite Weg genommen (`.import` mitverschoben, alle 117 Pfade
in `World/world.tscn`, `World/data/assets.tres` und den Modell-Szenen ersetzt und geprüft).

## Beim Benutzen beachten

- **FBX-Skalierung**: FBX wird oft in Zentimetern exportiert. Modell 100× zu groß → Datei
  anklicken → Reiter *Import* → `Scale` auf `0.01` → *Neu importieren*.
- **Kollision** erzeugt Godot nicht automatisch: beim Import unter *Meshes* erzeugen lassen oder
  in der Szene `StaticBody3D` + `CollisionShape3D` ergänzen.
- **Terrain3D-Instancer** liest mehrteilige Modelle als LOD-Stufen (daher `Nature/Forest/merged/`,
  erzeugt mit `scripts/Tools/merge_tree_meshes.gd`). Für normale Platzierung in der Szene egal.
- **Normal Maps**: Die MegaKits liefern zwei Konventionen. Im Projekt liegt überall die
  **Godot/Unity**-Variante aktiv; beim Village-Kit lag ursprünglich die Unreal-Version oben und
  wurde getauscht (Sicherung unter `Buildings/MedievalVillage/Textures/Normals-UnrealEngine/`).
  Erkennungsmerkmal der falschen Variante: Vertiefungen wirken wie Erhebungen.
