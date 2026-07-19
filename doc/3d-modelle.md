# 3D-Modelle — Arbeitsliste für dich

Alles im Spiel ist aktuell Platzhalter-Geometrie (Kapseln, Boxen, Kugeln in Grundfarben) — das
kann ich nicht durch echte Modelle ersetzen, das ist deine Aufgabe (3D-Modellierung selbst oder
über KI-Tools wie Meshy/Tripo AI/Rodin, siehe unser früheres Gespräch dazu). Diese Liste sagt dir
genau, was gebraucht wird, in welcher Größe/Farbe der aktuelle Platzhalter steht (damit du weißt,
was ungefähr ersetzt wird) und in welcher Reihenfolge es am meisten bringt.

**Format pro Modell**: Name → wo es aktuell als Platzhalter existiert (Datei/Maße/Farbe) → wofür
es gebraucht wird. Wenn du ein Modell hast: gib mir die Datei (`.glb`/`.fbx`/`.gltf`), ich baue es
in die passende `.tscn`-Datei ein — das ist dann wieder mein Teil.

**Wichtigster Rat zur Machbarkeit**: Nicht jede der ~30 Figuren braucht ein eigenes Unikat-Modell.
Sinnvoller: 3 Basis-Rigs (ein Nethora-Look, ein Freies-Lager-Look, ein Sekten-Look) mit
austauschbaren Texturen/Anbauteilen (Helm, Kapuze, Rüstungsteile) — das deckt die meisten NPCs ab.
Nur eine Handvoll Hauptfiguren brauchen wirklich ein eigenes Gesicht/Modell.

---

## Priorität 1 — Spieler & Kernfiguren (größter visueller Effekt zuerst)

- [ ] **Spielercharakter** — aktuell `Characters/Player.tscn`: `CapsuleShape3D`/`CapsuleMesh`
  ohne Textur. Menschlich, mittelalterlich, abgetragene Kleidung (Verurteilter, siehe
  [Spielercharakter.md](konzept/Charaktere/Spielercharakter.md)) — kein Rüstungsteil, da er ohne
  Ausrüstung startet. Braucht eine Laufanimation (mind. Idle/Walk/Run), da 3rd-Person-Kamera
  (`Characters/Player.tscn`, `scripts/Characters/Player.cs`) den Charakter durchgehend zeigt.
- [ ] **Cassian** (Ordensmeister, Nethora) — aktuell blaue Kapsel wie jeder NPC
  (`Characters/Npc.tscn`, Farbe `Color(0.2, 0.35, 0.65)`). Alt, würdevoll, Nethora-Gold/Weiß
  (siehe [Visueller-Stil.md](konzept/Praesentation/Visueller-Stil.md)), Ordensgewand.
- [ ] **König Aldemar** — wie Cassian als Basis, aber königlich/verfallen wirkend (siehe
  [Antagonisten.md](konzept/Charaktere/Antagonisten.md): "ein König, der gleichzeitig leugnet und
  sich fürchtet") — evtl. mit zunehmend ungepflegterem Look für spätere Szenen.
- [ ] **Renward** (Hauptmann/Torwache, Nethora) — Paladin-Rüstung, misstrauisch/wachsam wirkend.
- [ ] **Vox Aedificares** (Hochpriester, Sekte) — auffälligstes Sekten-Design, violett-rote Robe,
  Runen/Zeichen des Erbauers (siehe [Runen.md](konzept/Items/Runen.md)), sollte sich klar von
  allen anderen NPCs abheben (Hauptantagonist).
- [ ] **Grimbold** (informeller Anführer, Freies Lager) — grau/silbern, geflickte Ausrüstung,
  kampferprobt wirkend.
- [ ] **Ulf** (gefallener Altmagier, noch nicht in der Welt platziert — siehe
  [Antagonisten.md](konzept/Charaktere/Antagonisten.md)) — soll zunehmend entstellt/korrumpiert
  wirken (Blutmagie-Verfall). Wird erst gebraucht, wenn sein Bosskampf gebaut wird
  (siehe `doc/TODO.md`, noch nicht umgesetzt).

## Priorität 2 — 3 Fraktions-Basis-Rigs (deckt den Rest der NPCs ab)

- [ ] **Nethora-Rig**: Gold/Weiß, gepflegt aber angeschlagen (siehe
  [Visueller-Stil.md](konzept/Praesentation/Visueller-Stil.md): "Risse, Ruß, geflickte Stellen
  unter dem Gold"). Varianten für Rang: Vigil (leicht/Stoffwams), Ritter/Paladin (Plattenteile),
  Priester/Mönch (Robe). Deckt ab: Godric, Ivo, Meinhard, Lambert, Bertram, Hartwin, Bauer,
  Bürgerin, Bettler, Wache (10 Figuren).
- [ ] **Freies-Lager-Rig**: Grau/Silber, Leder + Schrott-Metall, improvisiert (siehe
  Visueller-Stil.md). Deckt ab: Torvald, Hagen, Ivar, Curran, Bandit, Schmuggler (6 Figuren).
- [ ] **Sekten-Rig**: Violett/Rot, Kutten/Roben (siehe Visueller-Stil.md). Deckt ab: Halvor, Kael,
  Malvern, Anhänger, Novize (5 Figuren, Vox Aedificares separat siehe oben).
- [ ] Austauschbare Anbauteile pro Rig (falls dein Workflow das hergibt): Kapuze/Helm an/aus,
  Waffe in der Hand, Schild am Rücken — spart, jede Figur einzeln zu bauen.

## Priorität 3 — Gegner

- [ ] **Mutierter Wolf** (`enemy_mutierter_wolf`) — aktuell rote Kapsel
  (`Characters/Enemy.tscn`). Vierbeiniges Tier, sichtbar entstellt/krank wirkend (siehe
  [Antagonisten.md](konzept/Charaktere/Antagonisten.md): Tiere mutieren durch Erbauer-Einfluss).
  Braucht Lauf-/Angriffsanimation.
- [ ] **Knochenwächter** (`enemy_knochenwaechter`) — gepanzert/knochig wirkend, passend zu seiner
  Rolle als "resistent gegen Schneidschaden" (siehe
  [Kampfsystem.md](konzept/Gameplay/Kampfsystem.md)).
- [ ] **Wegelagerer** (`enemy_wegelagerer`) — kann das Freies-Lager-Rig wiederverwenden
  (grob/bewaffnet), keine komplett neue Figur nötig.

## Priorität 4 — Waffen (aktuell alle unsichtbar, Schaden wirkt nur "unsichtbar")

Aktuell hat keine Waffe ein sichtbares Modell in der Spielwelt oder in der Hand — Schaden/Treffer
passieren rein rechnerisch (`scripts/Combat/Enemy.cs`, `Player.cs`). Sechs Grundformen decken alle
aktuellen Waffen-Items ab (siehe [Waffen.md](konzept/Items/Waffen.md) für die volle Liste,
`weaponCategory` in `Data/Items/*.json` für die Zuordnung):

- [ ] **Einhand-Schwert** (Basis-Mesh) — Materialvarianten: rostig (`sword_rusty`,
  `weapon_geflickter_saebel`), golden/graviert (`weapon_ordensklinge`), geweiht/dunkel
  (`weapon_opferdolch_schwert`)
- [ ] **Zweihand-Schwert** — `weapon_zeremonienschwert` (golden), `weapon_grobe_zweihandklinge`
  (grob/Freies Lager)
- [ ] **Stumpfwaffe/Keule** — `weapon_wachtkeule`, `weapon_soeldnerkeule`,
  `weapon_beschuetzerkeule`, `weapon_wanderstab`
- [ ] **Axt** — `weapon_custodes_streitaxt`, `weapon_rostige_axt`, `weapon_ritualaxt`
- [ ] **Bogen** — `weapon_grenzwaechter_bogen`, `weapon_jagdbogen`, `weapon_jaegerbogen`,
  `weapon_erweckerbogen`
- [ ] **Schild** (3 Varianten je Fraktionsfarbe) — `shield_ordensschild` (gold),
  `shield_behelfsschild` (grau/Holz), `shield_ritualschild` (violett)

## Priorität 5 — Rüstung (aktuell rein rechnerisch, kein sichtbarer Unterschied am Charakter)

Pro Fraktion 3-4 Rüstungsstufen (siehe
[Ruestung-Schilde.md](konzept/Items/Ruestung-Schilde.md)) — idealerweise als Anbauteile für die
Fraktions-Rigs aus Priorität 2, nicht als komplett eigene Modelle:
- [ ] Nethora: Vigil-Wams (leicht), Ritterharnisch (mittel), Paladinrüstung (schwer, am meisten
  Detail), Priestergewand (Robe)
- [ ] Freies Lager: Neulingsleder, Söldnerpanzer, Schattengewand, Altmagier-Robe
- [ ] Sekte: Neue-Kutte, Beschützerrüstung, Erwecker-/Seherrobe

## Priorität 6 — Gebäude & Welt (aktuell einfarbige Boxen)

Aus `Level/Zones/*.tscn` — Maße hier exakt wie im Platzhalter, damit ein neues Modell in der
Größenordnung passt (muss nicht exakt sein, Godot skaliert):

**Nethora:**
- [ ] Schloss — 10×5×8, aktuell altgold `Color(0.75, 0.7, 0.55)`, soll "einst prunkvoll, jetzt
  angeschlagen" wirken (siehe Visueller-Stil.md)
- [ ] Ordenssitz/Kaserne — 7×3×7, ähnlicher Goldton, etwas schlichter als das Schloss
- [ ] Taverne — 6×2.5×6, holzig `Color(0.6, 0.5, 0.35)`
- [ ] Marktstand (4× dieselbe Form reicht: Godric/Ivo/Meinhard/Lambert) — 3×2×3,
  `Color(0.55, 0.5, 0.45)`
- [ ] Wohnhaus (2× dieselbe Form reicht) — 4×3×4, `Color(0.65, 0.55, 0.4)`

**Freies Lager:**
- [ ] Lagerhöhle (Haupteingang/Fels-Look) — 12×4×10, `Color(0.5, 0.5, 0.52)`
- [ ] Trainingsbereich (offene Plattform, kein Gebäude nötig) — 5×1×5
- [ ] Altmagier-Bereich — 5×2×5, leicht violett-grau `Color(0.35, 0.3, 0.4)`
- [ ] Wachposten — 3×2×3, `Color(0.45, 0.45, 0.47)`

**Sektentempel:**
- [ ] Haupttempel — 9×4×9, dunkel-violett `Color(0.35, 0.15, 0.3)`, alter Erbauer-Tempel-Look
  (siehe [Orte.md](konzept/Welt/Orte.md))
- [ ] Ritualkammer — 4×2×4, dunkelrot `Color(0.4, 0.1, 0.15)`

**Angedeutete Orte (Ankunft-Zone, nur Fernwirkung nötig, kein Innenraum):**
- [ ] Küstenregion-Hinweis — 8×0.4×8 blaue Fläche `Color(0.2, 0.4, 0.6)`
- [ ] Mine-Zugang-Hinweis (2×, Nethora + Ankunft) — 3×3×2, dunkel `Color(0.15, 0.12, 0.1)`
- [ ] Alte-Kathedrale-Hinweis — 2×8×2, grauer Ruinenturm `Color(0.5, 0.5, 0.55)`
- [ ] Erbauer-Tempel-Hinweis (4× dieselbe Form reicht) — 1.5×3×1.5, violette Obelisken
  `Color(0.35, 0.1, 0.3)`

## Priorität 7 — Gegenstände/Props (klein, aber viele Wiederholungen im Spiel)

- [ ] **Item-Pickup-Leuchtkugel ersetzen** — aktuell jedes aufsammelbare Item ist dieselbe
  leuchtende Kugel (`Objects/Items/ItemPickup.tscn`, gelb-orange). Sinnvoller: pro Item-Typ ein
  kleines eigenes Modell statt der Kugel:
  - [ ] Trank-Fläschchen (Heiltrank/Segenswasser/Ausdauertrank, 3 Farbvarianten)
  - [ ] Kräuterbüschel (`herb_red`, `item_bittermoos`, `item_reine_quellblute`)
  - [ ] Rune/Siegelstein (`rune_fluchsiegel`, `rune_splitterrune`, `rune_bannrune`)
  - [ ] Brief/Schriftrolle (`item_koenigsbrief`, `item_wandbild_abdruck`)
  - [ ] Fragment/Artefakt (`item_siegel_fragment`, `item_ruinenfragment`, `item_ritualzutat`,
    `item_geweihter_gegenstand`)
- [ ] **Kiste** (`Objects/Crate.tscn`) — aktuell brauner Würfel, 1×1×1
- [ ] **Fels** (`Objects/Rock.tscn`) — aktuell graue Kugel, Radius 0.6

## Priorität 8 — UI-Icons (kein 3D, aber gehört zur selben "sieht noch nach Platzhalter aus"-Familie)

Inventar/Shop-Panel zeigen aktuell nur Text (`Hud.cs`), keine Icons. Sobald es 3D-Modelle gibt,
brauchen die meisten Items zusätzlich ein kleines 2D-Icon fürs Inventar (Render des 3D-Modells
oder eigene Illustration) — technisch ein neues Feld `ItemDefinition.IconPath` + Godot-
`TextureRect` im Inventar-UI. Das ist dann wieder mein Teil (Code), sobald Icons existieren.

---

## Wie es weitergeht

Sobald du ein Modell hast (egal welche Priorität), schick mir die Datei — ich baue sie in die
passende Szene ein (`.tscn` bearbeiten, Material/Textur zuweisen, Kollisionsform ggf. anpassen).
Du musst nicht die Liste von oben nach unten abarbeiten — was dir zuerst gelingt oder am meisten
Spaß macht, hat direkt sichtbaren Effekt im Spiel, weil aktuell wirklich alles Platzhalter ist.
