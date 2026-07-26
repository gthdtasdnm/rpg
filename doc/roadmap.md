# Roadmap – Gesamtplan (Stand 2026-07-26)

Was noch gebaut werden muss, bis das Spiel von Anfang bis Ende durchspielbar ist.
Sprint-Planung siehe `doc/sprint-2026-08-02.md`, Systemdetails siehe `doc/fortschritt-2026-07-25.md`.

---

## 1. Ausgangslage ehrlich

**Steht (Welt-Ebene, GDScript):** Terrain3D-Welt mit Bäumen/Steinen/Blockings, Gras, Wasser,
Tag/Nacht + Himmel, Partikel, Player (Laufen/Rennen/Springen/Schwimmen/3rd-Person-Kamera),
Baumkollision.

**Steht (Spiel-Ebene, C#) – wird nur gerade nicht benutzt:**
- Autoloads sind **noch registriert** (`project.godot:42-50`): `GameData`, `GameFlags`,
  `DialogueRunner`, `QuestManager`, `SaveSystem`.
- `UI/Hud.tscn` + `Hud.cs`: Leben, Interaktions-Prompt, Dialogbox, Questlog (`L`),
  Inventar (`I`), Pause-Menü (`Esc`), Speichern/Laden.
- Code: `Interactor`/`IInteractable`, `Inventory`, `ItemPickup`, `Npc`, `CharacterStats`,
  `Enemy`, `Equipment`, `SpellCaster`, `LocationTrigger`.
- **Inhalte in `Data/`: 30 NPCs, 27 Dialoge, 47 Items, 7 Quests, 4 Zauber** — bereits nach
  Konzept benannt (Aldemar, Cassian, Grimbold, Torvald, Königsbrief, Siegel-Fragment …).

**Die eigentliche Lücke:** die beiden Ebenen sind nicht verbunden. Es fehlen
`Characters/Npc.tscn`, das HUD in `Main.tscn`, und dem GDScript-Player fehlen
`CharacterStats` / `Inventory` / `Interactor`. Das ist **keine Neuentwicklung, das ist
Verkabelung** — deshalb steht es als M1 ganz vorne.

**Grundsatzentscheidung (Empfehlung):** C#-Framework behalten und wieder anschließen, Player
bleibt GDScript. Kein Neuschreiben in GDScript und keine Portierung des Players nach C#.
Begründung: das Framework ist fertig und getestet, die Daten sind da; ein Neuschreiben wirft
mehrere Wochen weg. Die Sprachgrenze kostet nur ein paar `call()`-Aufrufe.

---

## 2. Story-Struktur (verbindlich, ersetzt `doc/konzept/Story/Haupthandlung.md`)

```
Kapitel 1  Ankunft            → Barriere, erste Orientierung, Königsbrief an Nethora
Kapitel 2  Fraktionswahl      → Nethora | Freies Lager | Erleuchtete, Aufstieg im Rangsystem
Kapitel 3  Mittlere Aufgabe   → großer Einzelgegner (Drache o. ä.)
Kapitel 4  Die 5 Teleportsteine
              je Stein: Ort in der Welt aufsuchen → Teleport in Boss-Dungeon
              → Boss besiegen → Fragment
Kapitel 5  Finale             → 5 Fragmente am Tempel zusammensetzen
                              → Teleport in den Erbauer-Dungeon → Endkampf
```

### Mengengerüst, das daraus folgt
| Was | Anzahl |
|---|---|
| Außen-Orte mit Teleport-Schrein | 5 |
| Boss-Dungeons (Innenraum-Szenen) | 5 |
| Bosse (Modell + Animation + Kampf-KI) | 5 |
| Drachen-Arena + Drache | 1 |
| Tempel (Fragment-Zusammensetzung) | 1 |
| Erbauer-Dungeon + Endboss | 1 |
| Siedlungen (Nethora, Freies Lager, Tempel) | 3 |

⚠️ **Scope-Warnung:** 7 Dungeons + 7 einzigartige Bosse sind für ein Ein-Personen-Projekt sehr
viel — realistisch das Teuerste am ganzen Spiel. Zwei Gegenmaßnahmen, früh entscheiden:
1. **Dungeon-Kit statt Unikate**: ein modulares Set (Wand/Boden/Decke/Tür/Treppe/Säule) +
   3 Material-Varianten (Katakombe, Ruine, Erbauer-Tech) → aus einem Kit entstehen alle 5
   Boss-Dungeons, unterschiedlich nur in Layout und Licht.
2. **Bosse als Varianten**: 2–3 echte Boss-Skelette/Rigs, die 5 Bosse unterscheiden sich in
   Skalierung, Material, Fähigkeiten-Set und Arena — nicht in komplett eigenen Modellen.

Alternative, falls die Zeit knapp wird: **3 Steine statt 5**. Das ändert nur Zahlen in Quest-JSON
und Dialogen, nicht die Struktur. Diese Entscheidung sollte vor dem Bau von Kapitel 4 fallen,
nicht mittendrin.

---

## 3. Meilensteine

### M1 — Framework anschließen ⬅ Blocker für alles Inhaltliche
Ohne das kann kein NPC, keine Quest, kein Item getestet werden.
- [x] `World/npc.tscn` gebaut (StaticBody3D + Collider + Mesh + `Npc.cs`, mit Boden-Snap)
- [x] `Interactor` am Player, Prompt-Anzeige (`UI/InteractionPrompt.tscn`)
- [x] Dialog pausiert Spielerbewegung (`player.gd`)
- [x] Dialogsystem auf das **dialogue_manager-Addon** umgestellt, eigener `DialogueRunner` raus
- [x] CLAUDE.md korrigiert: Framework ist **nicht** verwaist, sondern abgekoppelt
- [x] Player: `CharacterStats` + `Inventory` + `Equipment` angehängt
- [x] `UI/Hud.tscn` in `Main.tscn` eingehängt (Leben, Silber, `I`, `L`, `Esc`, Händler)
- [x] `SaveSystem` an den GDScript-Player angebunden (`apply_save_state`)
- [x] NPCs sprechen den Spieler von selbst an (`GreetOnApproach`), drehen sich zu ihm
- [x] Währung auf **Silber** vereinheitlicht, Lehrer-System über `skill`-Items
- [ ] `ItemPickup` in der Terrain-Welt testen (Szene dafür fehlt noch)
- [ ] „Benutzen"-Knopf für Verbrauchsgüter im Inventar (aktuell nur „Ausrüsten")

**Damit ist M1 im Wesentlichen erledigt** — der Spielkern hängt am aktiven Player.

### M2 — Kampf & Gegner-Grundlagen
Voraussetzung für Drache, alle Bosse, Fauna.
- [ ] Animations-Pipeline entscheiden (Mixamo? eigene Rigs?) und **einmal** durchziehen
- [ ] Player: Angriff, Block, Treffer-Feedback, Tod/Respawn
- [ ] Waffen sichtbar am Charakter (Equipment → Mesh-Slot)
- [ ] Gegner-KI mit **limboai** (Behavior Trees, Addon ist schon da): Idle → Wahrnehmung →
      Verfolgen → Angreifen → Rückzug/Tod
- [ ] Schadensmodell: `IDamageable` + Waffen-/Rüstungswerte aus JSON verrechnen
- [ ] Loot-Drop beim Tod
- [ ] Gegner-Spawner (zonenbasiert, entfernungsabhängig aktiv)

### M3 — Fauna & Monster
- [ ] 4–6 Grundgegner: Wolf, Wegelagerer, Knochenwächter (JSON existiert bereits!), + 2–3 neue
- [ ] Friedliche Tiere (Ambiente): Reh, Vogel, Hase — reine Optik, kein Kampf
- [ ] Schwierigkeitsstufen pro Zone, damit die Welt lesbar gefährlich wird

### M4 — NPCs & Siedlungen
- [ ] Gebäude-Modelle: Kit für Nethora (Stadt), Freies Lager (Höhle/Holz), Tempel (Erbauer-Stein)
- [ ] Blockings durch echte Modelle ersetzen, 3 Siedlungen platzieren
- [ ] Innenräume: entscheiden ob begehbar oder Fassade (Empfehlung: nur wichtige Gebäude innen)
- [ ] NPCs in der Welt platzieren (`World/npc.tscn` instanzieren, `CharacterId` + `DialogueFile`)
- [ ] NPC-Daten neu schreiben — die 30 alten JSONs wurden am 26.07. geleert (siehe Sprint)
- [ ] Tagesabläufe (Gothic-Stil: Wegpunkte + Uhrzeit) — optional, aber stimmungsprägend
- [x] ~~Händler-System (Kaufen/Verkaufen)~~ — **existiert bereits** in `Hud.cs` (Kaufen/Verkaufen,
      Gold, Sortiment über `shopItemIds` im Charakter-JSON, Auslöser `do Dialog.OpenShop("id")`).
      Fehlt nur: das HUD wieder einhängen + Sortimente befüllen.
- [ ] Trainer-System (Skillpunkte gegen Lehrgeld) — Grundlage existiert: Items vom Typ `skill`
      setzen beim Kauf ein Flag `learned_<id>` statt in den Rucksack zu wandern

### M5 — Fraktionen & Progression
- [ ] Fraktions-Zugehörigkeit + Rang in `GameFlags`/Save
- [ ] Rangsysteme der 3 Fraktionen als Datenstruktur (Konzept ist fertig ausgearbeitet)
- [ ] Kapitel-1-Questkette bis zur Briefübergabe (Quests `quest_der_bote`, `quest_die_nachricht`
      existieren als JSON — prüfen und fertigstellen)
- [ ] Fraktionswahl als echter Wendepunkt (Beitritts-Quest je Fraktion, sperrt die anderen)
- [ ] Ruf/Ansehen, falls das Freie Lager es braucht
- [ ] Charakterwerte + Levelaufstieg + Skillpunkte-Verteilung (Konzept: `Gameplay/Progression.md`)

### M6 — Skills, Magie, Items
- [ ] Skill-System: Waffen-Skills (Einhand/Zweihand/Bogen), Kombos, Freischaltung über Trainer
- [ ] Magiesystem live: `SpellCaster` + die 4 vorhandenen Zauber + Mana/Ausdauer
- [ ] Runen (Konzept: `Items/Runen.md`)
- [ ] Ausrüstung anlegen/Werte wirken lassen (`Equipment.cs` ist da, wirkt aber noch nicht)
- [ ] Verbrauchsgüter (Tränke) wirksam machen
- [ ] Inventar-UI aufwerten (Icons statt Textliste)

### M7 — Kapitel 3–5: Dungeons, Teleport, Bosse
- [ ] **Zonen-/Szenenwechsel-System**: Teleport in Dungeon-Szene + zurück, Weltzustand bleibt
      erhalten (das ist neue Architektur, nicht trivial — früh anfangen)
- [ ] Dungeon-Kit modellieren (siehe Scope-Warnung oben)
- [ ] Drachen-Aufgabe (Kapitel 3) als erster Boss-Prototyp — testet M2 unter Realbedingungen
- [ ] 5 Teleport-Schreine in der Welt platzieren + Aktivierungs-Logik
- [ ] 5 Boss-Dungeons + 5 Bosse
- [ ] Tempel-Szene: Fragment-Zusammensetzung als Puzzle/Ritual
- [ ] Erbauer-Dungeon + Endkampf + Abspann

### M8 — Politur & Release-Reife
- [ ] **Performance**: aktuell ~86 % GPU auf einer 2080 Ti — Laptop-Ziel ist so nicht erreichbar.
      Grafik-Menü (Nebel, Gras-Dichte, Schattendistanz, Baumdistanz, Glühwürmchen-Lichter)
- [ ] Audio: Musik pro Zone, Ambiente, Kampf-/UI-Sounds, Schritte pro Untergrund
- [ ] Hauptmenü, Titelbild, Optionen, mehrere Speicherslots
- [ ] Balancing-Durchlauf über alle 5 Kapitel
- [ ] Export-Builds (Windows/Linux) + Testdurchlauf auf schwacher Hardware
- [ ] Aufräumen: `_zum_loeschen/`, `demo/`, `Models/terrain_3d.tscn`,
      `World/forest_particles.tscn`

---

## 4. Empfohlene Reihenfolge

```
M1 ─→ M2 ─→ M3 ─┐
      │          ├─→ M7 ─→ M8
      └→ M4 ─→ M5 ┘
              └→ M6
```

Kurz: **M1 zuerst** (billig, entsperrt alles). Dann **M2**, weil Kampf die Voraussetzung für
Kapitel 3–5 ist und die Animations-Pipeline die größte Unbekannte im Projekt darstellt — je
früher die einmal komplett durchgezogen ist, desto besser planbar wird der Rest. M4/M5/M6 laufen
danach parallel und sind gut in Einzelsitzungen zerlegbar. **M7 ist der größte Brocken** und
sollte erst starten, wenn ein Boss-Kampf (Drache) einmal spielbar funktioniert hat.

## 5. Doku-Aufgaben nebenbei
- [ ] `doc/konzept/Story/Haupthandlung.md` + `doc/konzept/Quests/Hauptquestreihe.md` auf die
      Struktur aus Abschnitt 2 umschreiben (aktuell veraltet)
- [ ] `doc/architektur.md` — beschreibt noch die gelöschte Szenenstruktur
- [ ] CLAUDE.md-Abschnitt "verwaist" korrigieren (siehe M1)
</content>
