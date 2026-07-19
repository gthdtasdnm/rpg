# Nächste Schritte — Story-Kapitel, Dialoge, Politur

Arbeitsliste für die nächste Runde (Auftrag: "genau geplant, ausführlich beschrieben, machbar").
Getrennt von [`doc/TODO.md`](TODO.md), das inzwischen v.a. ein Protokoll bereits erledigter
Arbeit ist — hier steht nur, was als Nächstes ansteht. Getrennt von
[`doc/3d-modelle.md`](3d-modelle.md), das deine eigene Aufgabenliste ist (3D-Modelle kann ich
nicht selbst bauen); diese Liste hier ist meine.

---

## Teil 1 — Story in Kapitel

[Haupthandlung.md](konzept/Story/Haupthandlung.md) beschreibt aktuell nur grobe Akte (1/2/3 +
Finale). Das wird hier in konkrete, nummerierte Kapitel heruntergebrochen, jedes mit Startpunkt,
Zielen, den tatsächlichen Quest-IDs und einem ehrlichen Status (schon gebaut / muss noch gebaut
werden). Jedes Kapitel bekommt außerdem ein Flag, das seinen Abschluss markiert — dient später
als Grundlage für Kapitel-Anzeige im Questlog (siehe Teil 3).

### Kapitel 1 — Ankunft
- **Status: bereits umgesetzt.**
- Start: Spielbeginn (`quest_der_bote` startet automatisch, `Player.cs`).
- Ziel: Orientierung, erster Kontakt (Gerald), optional erste Kämpfe (Wolf/Knochenwächter/
  Wegelagerer), Königsbrief + Schwert aufsammeln.
- Quests: `quest_der_bote` (läuft bis Kapitel 2), `quest_first_herbs` (Gerald, optional).
- Abschlussflag (neu anzulegen): `kapitel_1_abgeschlossen`, gesetzt beim ersten Betreten von
  Nethora (z.B. neuer `LocationTrigger` am Ortsübergang).

### Kapitel 2 — Der Bote
- **Status: bereits umgesetzt.**
- Start: Nethora betreten.
- Ziel: Cassian finden → Zugang zur Burg → Aldemar den Brief übergeben → Zusammenbruch miterleben.
- Quests: `quest_der_bote` (Abschluss bei Cassian), `quest_die_nachricht` (Abschluss bei Aldemar).
- Abschlussflag: bereits vorhanden (`quest_completed_quest_die_nachricht`), sollte zusätzlich
  `kapitel_2_abgeschlossen` setzen (Dialog-Choice bei Aldemars `breakdown`-Knoten ergänzen).

### Kapitel 3 — Erste Schritte in der Barriere
- **Status: Quests bereits umgesetzt, aber noch nicht als Kapitel gerahmt.**
- Start: nach Aldemars Zusammenbruch.
- Ziel: freie Erkundung aller drei Lager, erste Kontakte zu Kernfiguren, kleinere Aufträge
  erledigen.
- Quests: `quest_wegelagerer` (Renward), `quest_ruinenfragmente` (Torvald),
  `quest_ritualzutaten` (Kael), `quest_bettler_gefallen` (Bettler) — funktionieren unabhängig
  von Fraktionswahl.
- **Fehlt noch**: eine echte Orientierungs-/Einstiegsquest ("Erste Schritte in der Barriere" aus
  `Haupthandlung.md` Akt 1, bisher nie gebaut) — z.B. eine simple `talk_to_npc`-Kette, die den
  Spieler nacheinander zu Grimbold, Halvor und zurück zu Cassian schickt, bevor er sich bindet.
- Abschlussflag (neu): `kapitel_3_abgeschlossen`, manuell durch die neue Orientierungsquest
  gesetzt.

### Kapitel 4 — Bindung (Fraktionsbeitritt) — NEU, noch nicht gebaut
- Ziel: der Spieler entscheidet sich formell für eine Fraktion. Aktuell gibt es dafür **keinen
  echten Mechanismus** — die Quests aus Kapitel 3 sind fraktionsübergreifend lösbar, ohne dass
  sich der Spieler wirklich bindet.
- Zu bauen: eine Beitritts-Quest pro Fraktion (Nethora: formelles Gespräch mit Cassian; Freies
  Lager: Grimbold; Sekte: Halvor/eine neue Bewährungsquest), die am Ende ein Flag setzt, z.B.
  `fraktion_gewaehlt` mit Wert `nethora`/`freieslager`/`sekte` (kein reines GameFlags-Bool-Muster
  mehr — braucht einen kleinen Zusatz, siehe Teil 3, "Fraktions-Variable").
- Erst ab hier ergibt Kapitel 5 (Spezialisierung) und die fraktionsexklusiven Inhalte aus
  [Fraktionsquests.md](konzept/Quests/Fraktionsquests.md) wirklich Sinn.
- **Machbarkeit**: mittel — braucht die neue Fraktions-Variable (kleine Erweiterung), aber sonst
  nur neue Quest-/Dialog-JSONs nach bestehendem Muster.

### Kapitel 5 — Aufstieg & Spezialisierung — NEU, noch nicht gebaut
- Ziel: Rang-Aufstieg innerhalb der gewählten Fraktion (Kampf- vs. Magie-Zweig laut
  [Fraktionen.md](konzept/Welt/Fraktionen.md)), mehr Quest-Content pro Fraktion als aktuell (nur
  1 Nebenquest pro Fraktion existiert bisher).
- Nutzt bereits vorhandene Systeme (Handel/Training aus `doc/architektur.md`), braucht aber mehr
  Inhalt: 2-3 weitere Quests pro Fraktion, mehr benannte NPCs aus
  [NPCs.md](konzept/Charaktere/NPCs.md) (z.B. Dietrich/Roderich für Nethora, Piet/Sten für Freies
  Lager, Nym/Reth für die Sekte).
- **Machbarkeit**: hoch — reines Content-Volumen nach bestehendem Muster, kein neuer Code nötig.

### Kapitel 6 — Gefährliches Wissen (Ulf) — NEU, noch nicht gebaut
- Ziel: Ulfs Abdriften in die Blutmagie erleben (erst als Dialog/Beobachtung, dann Bosskampf),
  siehe [Antagonisten.md](konzept/Charaktere/Antagonisten.md).
- Zu bauen: Ulf existiert aktuell **nirgends im Spiel** (nur im Konzept). Braucht: NPC-Datensatz,
  Platzierung im Freien Lager (Altmagier-Bereich), eine kleine Quest-Vorgeschichte, und einen
  tatsächlichen Bosskampf (Ulf als `Enemy`-Datensatz mit höheren Werten als bisherige Gegner).
- **Machbarkeit**: mittel — nutzt das bestehende Kampfsystem 1:1, braucht nur die Dateneinträge
  + eine kleine Übergangsdialog-Kette (NPC → wird zu Gegner).

### Kapitel 7 — Das Siegel bricht (Vox Aedificares) — NEU, noch nicht gebaut
- Ziel: Vox Aedificares beginnt, "Neue" zu entführen — die Sekte wird von Randnotiz zu akuter
  Bedrohung.
- Zu bauen: eine sichtbare Konsequenz (z.B. ein NPC wie Halvor "verschwindet" aus der Welt —
  technisch: `Npc`-Node per Flag ausblenden/entfernen — plus eine neue Quest, die den Spieler zur
  Reaktion zwingt (retten/ignorieren/mitmachen, je nach Fraktion).
- **Machbarkeit**: niedriger — braucht neue Mechanik (NPC verschwindet dynamisch je nach
  Spielfortschritt), keine 1:1-Wiederverwendung bestehender Systeme.

### Kapitel 8 — Eskalation — NEU, noch nicht gebaut
- Ziel: alle Fäden laufen zusammen (Sekte, Freies Lager, Aldemars Rückzug schwächen das Siegel
  gemeinsam), siehe `Haupthandlung.md` Akt 3.
- **Machbarkeit**: niedrig — v.a. Schreibarbeit/Dialoge, aber baut auf allem vorherigen auf, macht
  erst nach Kapitel 4-7 Sinn.

### Kapitel 9 — Finale (4 Enden) — NEU, noch nicht gebaut
- Die 4 Enden aus `Haupthandlung.md` (Nethora/Freies Lager/Sekte/vierter Weg).
- **Machbarkeit**: niedrig bis jetzt nicht sinnvoll planbar — hängt komplett von Kapitel 4-8 ab,
  sollte erst konkretisiert werden, wenn der Rest steht.

### Empfohlene Reihenfolge
Kapitel 4 zuerst (schafft die fehlende Fraktions-Variable, die für den ganzen Rest gebraucht
wird), dann Kapitel 5 (reiner Content-Ausbau, hohe Machbarkeit), dann 6-9 in Ruhe.

---

## Teil 2 — Dialoge überarbeiten

Du hast recht, und ich will hier nicht einfach behaupten, es beim nächsten Versuch besser zu
treffen — das Risiko, dass es wieder "generisch" klingt, ist real, wenn ich einfach alle 27
Dialogdateien in einem Rutsch neu schreibe. Deshalb ein anderer Ablauf:

### Schritt 1 — Stilfragen klären (bevor irgendwas neu geschrieben wird)
Ich brauche von dir konkretes Feedback zu ein paar Fragen, bevor ich lospoliere, sonst rate ich
wieder nur:
- Was genau stört dich — zu blumig/pathetisch? Zu erklärend (Sätze, die aussprechen, was eh klar
  ist)? Zu gleichförmig (jeder NPC klingt gleich)? Zu kurz/oberflächlich? Zu wenig Auswahl bei
  den Antwortmöglichkeiten?
- Hast du ein Beispiel (aus einem anderen Spiel/Buch/Film) für einen Dialogton, der dem nahekommt,
  was du willst?
- Sollen die Antwortmöglichkeiten des Spielers vielfältiger werden (nicht nur "Ja, mach ich" /
  "Kein Interesse", sondern z.B. schnippische, misstrauische, neugierige Optionen)?

### Schritt 2 — 2-3 Pilot-Dialoge, bevor der Rest drankommt
Statt alle 27 Dateien auf einmal umzuschreiben: ich überarbeite zuerst **Cassian, Aldemar und
einen generischen NPC** (z.B. den Bettler) nach dem, was aus Schritt 1 rauskommt, du gibst
konkretes Feedback, dann erst geht's an den Rest. Das verhindert, dass ich denselben Fehler 27×
wiederhole, bevor du überhaupt einmal draufschauen konntest.

### Schritt 3 — Vollständige Liste, priorisiert
Sobald der Stil steht, in dieser Reihenfolge:
1. **Hauptstory** (höchste Priorität): `cassian_intro`, `aldemar_intro` — bereits Pilot, aber
   ggf. weiter verfeinern nach Kapitel-4-Inhalten.
2. **Fraktions-Kernfiguren**: `renward_intro`, `grimbold_intro`, `halvor_intro`,
   `voxaedificares_intro` — tragen viel erzählerisches Gewicht, verdienen mehr als eine Zeile.
3. **Questgeber**: `torvald_intro`, `kael_intro`, `bettler_1_intro` (Pilot) — haben schon
   Quest-Logik, brauchen nur bessere Prosa drumherum.
4. **Händler/Lehrer**: `godric_intro`, `ivo_intro`, `curran_intro`, `hagen_intro`,
   `meinhard_intro`, `lambert_intro`, `bertram_intro`, `ivar_intro`, `malvern_intro` — aktuell
   alle nach demselben Schema ("Zeig mir dein Sortiment" + eine Flavor-Zeile), am ehesten
   verzichtbar, aber sollen nicht alle gleich klingen.
5. **Generische Rollen-NPCs**: `bauer_1_intro`, `buergerin_1_intro`, `wache_1_intro`,
   `bandit_1_intro`, `schmuggler_1_intro`, `anhaenger_1_intro`, `novize_1_intro` — niedrigste
   Priorität (kurze Flavor-Zeilen), aber am einfachsten in großer Zahl zu verbessern, sobald der
   Stil aus Schritt 1/2 feststeht.
6. **Gerald** (`gerald_intro`) — ältester Dialog im Projekt, gleiche Überarbeitung verdient wie
   alles andere.

### Technische Anmerkung
`DialogueChoice` unterstützt aktuell nur lineares Verzweigen über `next`/Flags — falls du mehr
Auswahlvielfalt willst (z.B. eine schnippische Antwort, die den NPC anders reagieren lässt, statt
nur zum selben Folgeknoten zu führen), ist das mit dem bestehenden Schema machbar (mehr Knoten,
mehr `setFlag`-Verzweigungen), aber es lohnt sich, das an den Piloten aus Schritt 2 auszuprobieren
statt vorher zu planen.

---

## Teil 3 — Inventar, Stats, Quests: Politur

Konkrete Lücken, die beim genaueren Hinsehen auffallen — aktuell "irgendwie da", aber ohne
durchdachtes UI/UX:

### Inventar
- [ ] **Kategorien/Sortierung**: aktuell eine einzige flache Liste (`Hud.RefreshInventory`).
  Sollte nach Typ gruppiert werden (Waffen, Rüstung, Verbrauchsgüter, Questgegenstände, Sonstiges)
  — reine UI-Änderung in `Hud.cs`, keine neuen Daten nötig.
- [ ] **Ausrüstungsvergleich**: der "Ausrüsten"-Knopf zeigt nicht, ob das neue Item besser/
  schlechter ist als das aktuell getragene (z.B. "+5 Schaden" oder "-2 Verteidigung" als Vorschau).
  Braucht kleinen Vergleich zwischen `ItemDefinition` des aktuellen und des neuen Items.
- [ ] **Wegwerfen/Verwerfen**: aktuell kein Weg, unerwünschte Items loszuwerden außer Verkaufen
  (setzt einen Händler in der Nähe voraus). Kleiner "Wegwerfen"-Knopf wäre naheliegend.
- [ ] Icons: siehe [3d-modelle.md](3d-modelle.md) Priorität 8 — hängt an echten Modellen/Bildern.

### Stats
- [ ] **Es gibt aktuell KEINE Charakterbogen-Ansicht.** Strength/Dexterity sind im Spiel komplett
  unsichtbar (nur die Health-Bar zeigt etwas) — der Spieler kann seine eigenen Werte nirgends
  einsehen. Neues Panel nötig (`PanelKind.Character` nach demselben Muster wie Inventar/Shop):
  zeigt `Health`/`Strength`/`Dexterity` (aus `CharacterStats`/`CharacterDefinition`),
  ausgerüstete Items (`Equipment`), gelernte Fähigkeiten (`learned_*`-Flags aus `GameFlags` —
  braucht eine Liste bekannter Skill-IDs zum Anzeigen, aktuell nur `skill_onehanded_combo`/
  `skill_twohanded_combo`), Gold.
- [ ] Neue Taste dafür (z.B. `C` für "Charakter", analog zu `I`/`L`).

### Quests
- [ ] **Kapitel-Gruppierung im Questlog**: sobald Teil 1 (Kapitel-Flags) umgesetzt ist, sollte
  `Hud.RefreshQuestLog` Quests nach Kapitel/Fraktion gruppieren statt als eine lange Liste.
- [ ] **Haupt- vs. Nebenquest optisch unterscheiden**: aktuell sehen `quest_der_bote` und
  `quest_bettler_gefallen` im Questlog gleich aus. Neues Feld `QuestDefinition.IsMainQuest`
  (bool) + visuelle Hervorhebung.
- [ ] **Wegmarkierung/Kompass**: keine Anzeige, wo ein Questziel liegt (relevant, jetzt wo die
  Welt durchgehend groß ist, siehe `doc/architektur.md` "Welt"). Kleinster sinnvoller erster
  Schritt: Questlog-Eintrag zeigt den Namen des Gebiets, in dem der Questgeber steht.

---

## Reihenfolge-Empfehlung insgesamt

1. **Teil 2, Schritt 1** zuerst (Stilfragen klären) — ohne das verschwende ich sonst wieder Zeit
   mit derselben Art Dialog, die dir nicht gefällt.
2. Danach **Teil 2, Schritt 2** (Pilot-Dialoge), mit deinem Feedback.
3. Parallel dazu ist **Kapitel 4** (Teil 1) unabhängig angehbar, da rein strukturell.
4. Inventar/Stats/Quests-Politur (Teil 3) ist jederzeit einschiebbar, hängt an nichts anderem.

Sag Bescheid, womit ich anfangen soll — oder beantworte erstmal die Stilfragen aus Teil 2,
Schritt 1, dann leg ich los.
