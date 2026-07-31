# Spielsysteme

Kampf, Magie, Progression, Items. **Übergeordnet gilt `doc/Kapitelverlauf.txt`.**
Welt und Figuren stehen in `doc/welt.md`, Technik in `doc/technik.md`.

⚠️ **Nichts davon ist implementiert außer Items, Inventar, Ausrüstung (ohne Wirkung) und Stats.**
Kampf, Magie und Progression sind Konzept. Zahlenwerte fehlen absichtlich — sie entstehen erst
beim Bauen; die Waffen in `Data/Items/` haben inzwischen welche, aber ungetestet (siehe
Abschnitt 4).

---

## 1. Kampf

**Grundprinzip:** timing- und kombinationsbasiert wie in Gothic, kein Button-Mashing. Angriffe
haben spürbare Vor- und Nachlaufzeiten, in denen der Charakter festgelegt ist. Blocken und
Ausweichen müssen vorausschauend getimt werden, nicht reaktiv aus jeder Position.

| Kategorie | Schadensart | Skaliert mit |
|---|---|---|
| Einhand-Schwert | Schneid | `strength` |
| Zweihand-Schwert | Schneid, höher aber langsamer | `strength` |
| Stumpfwaffen (Keule, Morgenstern) | Stoß | `strength` |
| Äxte | Misch aus Schneid und Stoß | `strength` |
| Bogen | Fernkampf, eigener Wert | `dexterity` |
| Schild | kein Schaden | Verteidigung |

**Resistenzen entscheiden die Waffenwahl:** ungepanzerte/organische Gegner (mutierte Tiere) sind
anfälliger für **Schneidschaden**, gepanzerte und knochige Gegner (Untote, gerüstete
Sektenwächter, **Abaddon**) für **Stoßschaden**. Äxte sind als Mischwaffe generalistisch, aber
nie optimal — echte Spezialisierung lohnt gegnerabhängig.

**Blocken:** Mit Schild stark reduzierter Schaden und geringes Stagger-Risiko. Ohne Schild
möglich, aber schwächer — Block bricht leichter durch, ein Teil des Schadens kommt trotzdem an.

**Stagger:** Wer gestaggert ist, ist kurz handlungsunfähig — offenes Fenster für einen Konter.
Zweihand- und Stumpfwaffen staggern stärker, holen dafür langsamer aus.

**Offen:** wie Magie in den Nahkampf-Loop eingreift, insbesondere ob gegnerische Treffer die
göttliche Kanalzeit unterbrechen.

---

## 2. Magie

**Die eine Regel, die alles trägt: Magie kostet Leben, nicht Mana.** Es gibt keine abstrakte
Ressource. Das ist bewusst so — „Macht kostet immer etwas" ist ein Kernthema (siehe
`doc/welt.md`).

### Alte Magie / Kampfmagie

Feuer, Eis, arkane Energie. Kostet Leben beim Wirken, dafür **sofort einsetzbar mitten im Kampf**
— die aggressive, riskante Schule. Erlernt über das Freie Lager (Lehrling → Wissenssucher →
Altmagier), Lehrer ist **Torvald**.

⚠️ **Lore-Konsequenz:** Das Nutzen dieser Magie **schwächt Azazels Siegel.** Erzählerisch ein
Hebel — je mehr der Spieler sie einsetzt, desto sichtbarer könnte die Welt verfallen.

Vorschläge: **Feuerball** (Schaden über Zeit, günstig) · **Eisspeer** (hoher Einzelschaden,
verlangsamt/staggert) · **Arkane Klinge** (Waffenverzauberung mit Rüstungsdurchdringung statt
Direktschaden).

### Blutmagie

Untergruppe der alten Magie, extremer: höherer Lebenspreis, kann aber Leben **zurückgeben** —
nicht als Heilung, sondern als Umweg über den Gegner. Beispiel **Fluchsiegel**: kostet die Hälfte
des aktuellen Lebens, zieht dafür pro Sekunde von jedem Gegner im Siegel Leben zum Wirkenden.
Netto-Gewinn nur bei mehreren Gegnern, Netto-Verlust bei einem.

Muss spürbar **riskanter** sein als reine Kampfmagie, nicht einfach eine bessere Version. **Ulf**
zeigt als Zwischengegner, was bei sorglosem Einsatz passiert.

### Göttliche Magie / Gebete

**Lange Kanalzeit, Bewegung komplett gesperrt** — dadurch ungeeignet für den offenen
Schlagabtausch, gedacht für Vorbereitung und Nachsorge. Heilung, Buffs, und Angriffe, die
**speziell gegen Dämonisches** wirken. Erlernt über Nethora (Novize → Mönch → Priester), Lehrer
**Ansel**, Einstieg bei **Wendelin**.

Vorschläge: **Segnung** (heilt über Zeit) · **Rüstung des Glaubens** (temporärer Rüstungsbonus) ·
**Bann gegen das Böse** (nur gegen Dämonisches wirksam, gegen Menschen und Tiere wirkungslos).

**Hier hängen die beiden Story-Formeln dran** (Wortlaut in `doc/welt.md`, Abschnitt 6):
- **„Satis est. Contine manum tuam."** — hält die Turmengel in Kapitel 3 an und heilt sie.
  Kein normaler Zauber, sondern ein Fundstück; muss erst gefunden werden.
- **„Quod fractum est, integrum fiat."** — die *Benedictio Integritatis*, die Salomo in Kapitel 4
  braucht, um die Fragmente zu verbinden.

Beide sollten sich mechanisch wie Gebete anfühlen (Stillstand, Kanalzeit) — das macht den
Turmkampf in Kapitel 3 zu etwas anderem als jeden anderen Kampf im Spiel: **man muss stehen
bleiben, während etwas auf einen einschlägt.**

### Erweckungsrituale der Erleuchteten

Nutzen direkt Azazels Magie. Gelehrt von **Voss**. **Offen**, ob der Spieler sie lernen kann oder
sie rein narrativ/gegnerseitig bleiben.

---

## 3. Progression

**Kein Skilltree-Screen.** Gothic-Stil: Attribute (`Health`, `Strength`, `Dexterity`) steigen über
Lernpunkte, **neue Fähigkeiten lernt man bei NPC-Lehrern gegen Bezahlung oder Gefallen** — nicht
automatisch per Levelaufstieg.

Fähigkeiten sind technisch **Items vom Typ `skill`**; gelernt wird als Flag `learned_<id>`, nichts
landet im Rucksack.

**Lehrer:** Bertram (Nethora, allgemein) · Hagen (Freies Lager) · Malvern (Erleuchtete) · Ivar
(Zweihand/Äxte) · Roswin, Osric (Bogen, unabhängig) · Sten (Schleichen/Schlösser) · Torvald (alte
Magie) · Ansel, Wendelin (Gebete) · Voss (Erweckungsrituale) · Falk (`skill_bogen`), Knud
(`skill_haeuten`) je 100 Silber

**Fraktionsexklusiv** — die Wahl schließt Fähigkeiten dauerhaft aus:
- Nur das **Freie Lager** lehrt Diebstahl/Schleichen und alte Magie/Blutmagie
- Nur **Nethora** lehrt formale göttliche Magie in voller Tiefe
- Nur die **Erleuchteten** lehren Erweckungsrituale

Kein Grinden aller Fähigkeiten in einem Durchgang — eine echte Entscheidung.

**Tempo:** langsam und spürbar, jeder Punkt eine bewusste Investition. Kein Levelcap.

**Inventar: keine Kapazitätsgrenze** — kein Gewichtslimit, kein Slot-Cap. Bewusste Entscheidung
gegen Inventar-Management als Spielmechanik. Entspricht der bestehenden `Inventory`-Klasse.

**Währung ist Silber** (`Inventory.Silver`, Feld `price` in den Item-JSONs).

---

## 4. Items

Alles Inhaltliche sind **Daten, kein Code**: neues Item → JSON unter `Data/Items/`. Dateien mit
`_` am Anfang ignoriert `GameData` (Vorlagen).

**Steht:** Inventar als anklickbares Raster (`UI/InventoryScreen.tscn`) mit Kategorie-Reitern,
Ausrüstungsslots, Detailspalte und den Aktionen Ausrüsten/Ablegen/Benutzen/Wegwerfen · Icons, die
aus dem 3D-Modell des Items gerendert statt gemalt werden · Items in der Welt platzieren, aufheben
und wieder fallen lassen · Ausrüstung sichtbar am Charakter und im Savegame. Wie das zusammenhängt,
steht in `Data/README.md`.

**Offen:** Die Waffenwerte unten sind erste Zahlen zum Anfassen, kein austarierter Satz — Kampf
und Resistenzen (Abschnitt 1) gibt es noch nicht, `damage`/`defense`/`staggerResist` wirken also
bisher nirgends. Rüstung und Schild haben noch kein eigenes Modell am Charakter, nur die Waffe.

### Waffen

Optik je Fraktion: Nethora Gold/Weiß verziert · Freies Lager grau/silber geflickt · Erleuchtete
violett/rot rituell.

| Fraktion | Beispiele |
|---|---|
| **Nethora** (Godric) | Ordensklinge (Einhand) · Zeremonienschwert (Zweihand, teuer) · Wachtkeule (Startausrüstung Vigil) · Custodes-Streitaxt · Grenzwächter-Bogen |
| **Freies Lager** (Curran) | Geflickter Säbel · Grobe Zweihandklinge (Loot, selten) · Söldnerkeule (Startausrüstung) · Rostige Axt · Jagdbogen |
| **Erleuchtete** (Wrathe) | Opferdolch-Schwert · Ritualaxt (Beschützer-exklusiv) · Beschützerkeule · Erweckerbogen |
| **Unabhängig** | Jägerbogen (Roswin/Osric) · Wanderstab (Loot) |

### Rüstung & Schilde

An Rang gebunden — je höher der Rang, desto vollständiger, aber sichtbar geflickt und
angeschlagen.

| Fraktion | Sets (Schutz / Stagger) |
|---|---|
| **Nethora** | Vigil-Wams (niedrig / leicht) · Ritterharnisch (mittel / etwas langsamer) · Paladinrüstung (hoch / hohe Resistenz, spürbar langsamer) · Priestergewand (sehr niedrig / keine Einschränkung bei Gebeten) |
| **Freies Lager** | Neulingsleder (niedrig) · Söldnerpanzer (mittel) · Schattengewand (niedrig, dafür leise, Schleichbonus) · Altmagier-Robe (sehr niedrig, keine Einschränkung) |
| **Erleuchtete** | Neue-Kutte (sehr niedrig) · Beschützerrüstung (mittel-hoch, einzige echte Panzerung der Sekte) · Erwecker-/Seherrobe (niedrig, Ritualbonus) |

Schilde: Ordensschild (groß, hohe Deckung) · Behelfsschild (Schrott/Holz, mittel) · Ritualschild
(selten, nur Beschützer).

### Runen

Physische Träger für Blutmagie und alte Magie — geritzte Steine, die ein Ritualmuster binden.
Beim Wirken verbraucht (`type: material`). Kosten und Effekt bleiben in den Zauberregeln; die Rune
ist nur der Auslöser. Nicht sektenexklusiv — die Altmagier nutzen sie auch, beide zapfen dieselbe
alte Magie an.

| Rune | Effekt | Beschaffung |
|---|---|---|
| Fluchsiegel-Rune | zieht Leben von Gegnern im Wirkbereich | Skarn, selten Doyle |
| Splitterrune | verstärkt einen Kampfzauber einmalig | Torvald lehrt die Herstellung |
| Bannrune | gegen Azazels Kreaturen gerichtet — Brücke zur göttlichen Magie | Fund in Ruinen/Katakomben |

**Offen:** frei kombinierbar (Baukasten) oder fest definierte Zauber? Letzteres ist einfacher und
passt besser zum datengetriebenen Item-Schema. Crafting ist bisher nicht vorgesehen.

### Verbrauchsgüter

Kräuter: Rotes Kraut (`herb_red`, existiert) · Bittermoos (Blutmagie-Zutat, Skarn) · Reine
Quellblüte (göttliche Segnungen, Meinhard).

Tränke: Heiltrank (Meinhard, Doyle) · Ausdauertrank (Doyle, Curran) · Segenswasser (kleiner
Rüstungsbonus, geweiht, Ulrich).

### Questgegenstände der Hauptquestreihe

Direkt aus `doc/Kapitelverlauf.txt` abgeleitet:

| Item | Kapitel |
|---|---|
| **Königsbrief** | 1 — Ausgangsitem. Übergabe in Nethora, *oder* beim Freien Lager abgeben und damit Nethora hintergehen |
| **Heiliges Buch** | 1 — Beitrittsquest Nethora (finden) und Sekte (finden, aber zur Sekte bringen) |
| **Abaddons Manuskript** | 2 — hält fest, was wirklich vorgeht. Alle drei Lager wollen es |
| **Das Gebet** *„Satis est…"* | 3 — Fundstück, ohne das die Türme nicht zu bewältigen sind |
| **Teleportstein-Fragment ×5** | 3 — je eines pro geheiltem Turmengel |
| **Benedictio Integritatis** | 4 — der Segen, den Salomo braucht |
| **Der Teleportstein** | 5 — zusammengesetzt, führt zu Azazel |

Fraktions-Questitems (Beispiele): Ruinenfragment (Otbert/Nils) · Ritualzutat (Kael) · Geweihter
Gegenstand (Ulrich).

---

## 5. Was noch nicht implementiert ist

Vollständige Liste, damit klar ist, was Konzept und was Code ist:

- **Kampf komplett** — Angriff, Block, Treffer-Feedback, Tod/Respawn, Schadensmodell,
  Waffen sichtbar am Charakter
- **Animationen** — Pipeline noch nicht entschieden (Mixamo oder eigene Rigs)
- **Gegner-KI** — `limboai` ist als Addon vorhanden, aber unbenutzt
- **Magie** — `SpellCaster` existiert als Klasse, wirkt aber nicht
- **Ausrüstungswirkung** — `Equipment.cs` existiert, die Werte wirken noch nicht
- **Verbrauchsgüter** — kein „Benutzen"-Knopf im Inventar, nur „Ausrüsten"
- **Trainer-System** — Grundlage da (`skill`-Items setzen `learned_<id>`), Lehrer fehlen
- **Levelaufstieg, Attributsverteilung, Skills**
- **Zonenwechsel/Teleport** — Voraussetzung für Kapitel 3 und 5
- **Fraktionszugehörigkeit und Rang** in `GameFlags`/Save
- **Loot-Drop, Gegner-Spawner**
- **Fauna** — Wolf, Wegelagerer, Knochenwächter, friedliche Tiere als Ambiente
