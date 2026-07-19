# TODO — Spielumsetzung aus dem Konzept

Arbeitsliste für "Spiel aus dem Konzept bauen" (Auftrag vom 2026-07-18). Wird laufend
abgehakt; nach jedem abgeschlossenen Punkt kommt ein Kurzeintrag in
[`doc/backlog/2026-07-18.md`](backlog/2026-07-18.md). Wenn die Bearbeitung unterbrochen wird:
**hier weiterlesen** — der Abschnitt "Zuletzt / Nächster Schritt" ganz unten zeigt den genauen
Stand.

Reihenfolge bewusst gewählt: Daten zuerst (risikoarm, additiv), dann Kampf (größte fehlende
Systemlücke laut `doc/anforderungen.md`), dann Magie, dann Zonen/Welt, zuletzt Politur.
Nicht jeder NPC aus `doc/konzept/Charaktere/NPCs.md` wird einzeln in die Welt gesetzt — das
wären >90 Figuren. Stattdessen: eine repräsentative Auswahl pro Fraktion (Questgeber/Lehrer/
Anführer), sodass jede Zone bespielbar ist. Rest bleibt als Konzept-Reserve für später.

## Milestone 1 — Daten (JSON), additiv zu Data/*

- [x] Item-Schema erweitern (`ItemDefinition.cs`): Felder für Rüstung (`Defense`,
  `StaggerResist`), Waffen-Schadensart (`DamageType`: slashing/blunt/mixed/ranged), Consumables
  (`HealAmount`) — kein eigenes `Slot`-Feld nötig, `Type` selbst unterscheidet weapon/armor/shield
- [x] Waffen aus `Items/Waffen.md` als `Data/Items/weapon_*.json` (+ `shield_*.json`)
- [x] Rüstung/Schilde aus `Items/Ruestung-Schilde.md` als `Data/Items/armor_*.json`
- [x] Verbrauchsgüter/Questitems aus `Items/Verbrauchsgueter-Questitems.md` als `Data/Items/*.json`
- [x] Runen aus `Items/Runen.md` als `Data/Items/rune_*.json`
- [x] Kern-NPC-Roster (Auswahl, s.o.) als `Data/Characters/npc_*.json` (13 NPCs: Cassian,
  Aldemar, Renward, Godric, Ivo, Hartwin, Grimbold, Torvald, Hagen, Curran, Kael, Halvor,
  Vox Aedificares)
- [x] Dialoge für die Kern-NPCs als `Data/Dialogues/*.json`
- [x] Akt-1-Hauptquestkette (Teil 1) als `Data/Quests/*.json`: `quest_der_bote` (Cassian
  aufsuchen) → `quest_die_nachricht` (Brief an Aldemar übergeben, löst Zusammenbruch aus). "Erste
  Schritte in der Barriere" (Orientierungsquest) noch offen, niedrige Priorität.
- [x] Nebenbei: `QuestManager` um Objective-Typen `talk_to_npc`/`reach_location` erweitert (war
  Voraussetzung für die obige Kette, `collect_item` allein reichte nicht) — dokumentiert in
  `doc/architektur.md`
- [x] `quest_der_bote` startet automatisch beim Spielstart (`Player.cs`), passend zur
  Ausgangssituation ("Spieler ist bereits Bote, nicht erst durch NPC beauftragt")

## Milestone 2 — Kampfsystem (`scripts/Combat/`) — ABGESCHLOSSEN (Code-seitig)

- [x] Gegner-Felder additiv in `CharacterDefinition` ergänzt (keine eigene `EnemyDefinition` —
  siehe Kommentar in der Datei, gleiche Datenquelle wie NPCs, nur andere Felder befüllt)
- [x] `Enemy.cs` (CharacterBody3D): einfache KI (Aggro-Radius, Verfolgen, Angriff), nutzt
  bestehendes `CharacterStats`, Gruppe `"enemies"`
- [x] ~~Auto-Equip~~ → **ersetzt durch manuelles Equip-UI, siehe Milestone 6.**
- [x] Nahkampfangriff: Input-Actions `attack_left`/`attack_right` (Maus links/rechts),
  `MeleeHitbox`-Area3D am Player, Schaden bei Tastendruck sofort ausgewertet (kein Windup/Aktiv-
  Fenster mangels Animationen) — **seit Milestone 6 zwei Tasten statt einer, siehe dort**
- [x] Blocken (Input-Action `block`, jetzt Taste `B`) mit/ohne Schild + Stagger-Zustand
  (Spieler UND Gegner), Schadensart-vs-Resistenz-Matrix (`WeakTo`/`ResistantTo` in `Enemy.cs`)
- [x] Item-Drop beim Gegnertod (`Enemy.OnDied` instanziiert `ItemPickup` an der Sterbeposition)
- [x] 2 Gegnertypen angelegt (`enemy_mutierter_wolf` organisch/weak-to-slashing,
  `enemy_knochenwaechter` gepanzert/weak-to-blunt) + im Testlevel platziert
- [ ] **Spielbarkeit NICHT geprüft** — kein Godot-Editor/-Binary in dieser Umgebung verfügbar,
  nur `dotnet build` (C#-Kompilierfehler) und manuelle `.tscn`-Konsistenzprüfung möglich. Echter
  Playtest (Kampfgefühl, Balance, Szenendatei-Syntaxfehler die `dotnet build` nicht findet) steht
  noch aus — **wichtigster offener Punkt für die nächste Session mit Editor-Zugriff.**

## Milestone 3 — Magiesystem (`scripts/Magic/`) — ABGESCHLOSSEN (Code-seitig)

- [x] `SpellDefinition` + `Data/Spells/*.json` (4 Zauber: Feuerball, Eisspeer, Fluchsiegel, Segnung)
- [x] `SpellCaster.cs`: nutzt `CharacterStats.CurrentHealth` als Ressource (siehe
  `doc/architektur.md`)
- [x] Alte Kampfmagie: Feuerball, Eisspeer (Sofortkosten, sofort einsetzbar, kein echtes Projektil)
- [x] Blutmagie: Fluchsiegel (hohe Kosten, periodischer Lebensrückgewinn im Wirkbereich, 4s Dauer)
- [x] Göttliche Magie: Segnung (Kanalzeit + Bewegungssperre über `Player.IsChanneling`-Check,
  Heilung nach Kanalende)
- [x] Cast-Input (`cast_spell_1/2/3`, Tasten 1/2/3) — **keine HUD-Anzeige** (Cooldown/aktiver
  Zauber nicht sichtbar, siehe "offene Punkte" unten)

## Milestone 4 — Welt/Zonen (`Maps/`, `Level/`) — ABGESCHLOSSEN (Code-/Daten-seitig)

- [x] `ZoneTransition.cs` + `ZoneManager.cs` (neuer Autoload) + `Main.cs`: **Architektur-Änderung**
  — `Player`/`Hud` leben jetzt dauerhaft in `Level/Main.tscn` (neue `run/main_scene`), Zonen sind
  eigene Szenen unter `Level/Zones/*.tscn` ohne eigenen Player, die zur Laufzeit in
  `Main.ZoneRoot` ausgetauscht werden. Grund: ein reines `ChangeSceneToFile()` hätte Player
  zerstört und damit Inventory/Equipment/SpellCaster/aktuelles Leben verloren, da diese als
  Player-Kindnodes leben, nicht als Autoload. Siehe Kommentar in `ZoneManager.cs`.
- [x] Zone "Nethora" (Blockout: Schloss, Ordenssitz, Taverne, 2 Marktstände) + Kern-NPCs (Cassian,
  Aldemar, Renward, Godric, Ivo, Hartwin) — zentraler Knotenpunkt, Übergänge zu Ankunft/Freies
  Lager/Sektentempel
- [x] Zone "Freies Lager" (Blockout: Lagerhöhle, Trainingsbereich, Altmagier-Bereich) + Kern-NPCs
  (Grimbold, Torvald, Hagen, Curran)
- [x] Zone "Sektentempel" (Blockout: Haupttempel, Ritualkammer) + Kern-NPCs (Halvor, Kael,
  Vox Aedificares)
- [x] `Testlevel.tscn` → `Level/Zones/Ankunft.tscn` migriert (Gerald, Wolf, Knochenwächter,
  Königsbrief-Pickup bleiben erhalten), Player/Hud draus entfernt (leben jetzt in `Main.tscn`),
  `Testlevel.tscn` gelöscht (Inhalt lebt vollständig in `Ankunft.tscn` weiter)
- [x] Alle 4 Zonen bidirektional verkettet: Ankunft ↔ Nethora ↔ Freies Lager, Nethora ↔
  Sektentempel. **Übergänge NICHT getestet** (kein Godot-Editor, siehe unten).

## Milestone 6 — Playtest-Feedback (erste Spielrunde durch den Nutzer)

Nutzer hat das Spiel getestet: Laufen/Quest/Dialoge funktionieren. Rückmeldung: Gegner greifen
sofort beim Spawn an (zu nah), keine Möglichkeit Waffen/Runen manuell auszurüsten, Kampfsystem
sollte Links-/Rechtsklick-Wechsel für "flüssige" Treffer vorbereiten (Grundlage für späteres
Timing-/Kombosystem, freigeschaltet durch Einhandwaffen-Training).

- [x] Gegner in `Ankunft.tscn` von Spawn wegbewegt (Wolf/Knochenwächter standen näher am Spawn als
  ihr eigener `AggroRadius` — direkter Sofortangriff beim Levelstart). Jetzt nahe der
  Nethora-Zonengrenze, mit sicherem Laufweg dazwischen.
- [x] `Equipment.cs`: Auto-Equip entfernt, ersetzt durch `Equip(itemId)` (manuell, sortiert nach
  `ItemDefinition.Type` in weapon/shield/armor-Slot) + neuer `EquippedArmorId`-Slot.
- [x] `Hud.RefreshInventory`: jede ausrüstbare Zeile bekommt einen "Ausrüsten"-Knopf, aktuell
  ausgerüstetes Item zeigt "(ausgerüstet)" statt Knopf. Runen bleiben bewusst ohne Knopf
  (Zauber-Reagenz, keine Ausrüstung, siehe `doc/konzept/Items/Runen.md`).
- [x] `Player.TakeDamage` zieht jetzt `Equipment.EquippedArmorId`-Defense flach ab (Feld existierte
  in den Daten, wurde vorher nirgends angewendet).
- [x] Kampf-Input auf zwei Tasten aufgeteilt: `attack_left`/`attack_right` (Maus links/rechts),
  `block` auf Taste `B` verschoben (Maus rechts war jetzt durch `attack_right` belegt).
- [x] Erste Combo-Stufe: abwechselnde Links/Rechts-Treffer zählen als "flüssig" (kürzerer
  Folge-Cooldown + kleiner Schadensbonus pro Stufe, `_comboCount`/`_comboWindowTimer` in
  `Player.cs`), zweimal dieselbe Taste oder zu langsames Nachlegen setzt zurück und ist spürbar
  träger. **Noch kein echtes Timing-Fenster, keine Freischaltung durch Waffentraining** — das
  ist die als "später" angekündigte Ausbaustufe, absichtlich noch nicht gebaut.
- [x] `sword_rusty` zusätzlich als direkt aufsammelbares `ItemPickup` in `Ankunft.tscn` platziert
  (vorher nur Questbelohnung) — direkter Testpfad "Schwert aufheben → ausrüsten → Gegner
  angreifen" ohne erst die Kräuterquest abschließen zu müssen.
- [x] `dotnet build`: 0 Fehler, 0 Warnungen. **Weiterhin ungespielt** (kein Godot-Editor) — diese
  Runde ist reine Reaktion auf gemeldetes Spielverhalten, nicht auf eigene Beobachtung.

## Milestone 5 — Politur/Integration

- [x] `dotnet build` sauber (0 Fehler/Warnungen) nach jedem Milestone — durchgehend eingehalten
- [x] `doc/anforderungen.md` Checkboxen aktualisieren
- [x] Zusätzlich (nicht ursprünglich geplant, aber beim Bauen aufgefallen): Bug in
  `SaveSystem`/`ZoneManager` gefixt — ohne `SaveData.CurrentZoneScenePath` hätte ein in Nethora
  gespeicherter Spielstand nach dem Laden falsche Zonen-Geometrie unter den gespeicherten
  Koordinaten gehabt. Siehe `doc/architektur.md` Abschnitt "Zonen".
- [x] Manuelle `.tscn`-Konsistenzprüfung: alle Ext-/SubResource-Referenzen in allen geänderten
  Szenen automatisiert gegengecheckt (kein hängender/fehlender Verweis), alle `GetNode<T>("Pfad")`
  Aufrufe im C#-Code gegen die tatsächlichen Node-Namen in den `.tscn`-Dateien verglichen.
- [ ] **Kompletter Testdurchlauf im Editor: NICHT gemacht.** Kein Godot-Binary/-Editor in dieser
  Umgebung verfügbar (weder `godot` noch `godot4` im PATH, kein `.godot/`-Editor-Cache mit
  Binary). Alles in diesem Dokument als "code-seitig abgeschlossen" markierte ist nur
  kompilier- und strukturgeprüft, **nie tatsächlich gespielt worden.**
  **→ Wichtigster nächster Schritt für eine Session mit Editor-Zugriff:** Projekt öffnen, Main.tscn
  starten, Ankunft → Nethora → (Freies Lager/Sektentempel) durchlaufen, Cassian/Aldemar-Dialog
  (Akt-1-Quest) durchspielen, gegen Wolf/Knochenwächter kämpfen (Nahkampf + Blocken), alle 3
  Zauber wirken, speichern/laden testen (auch zonenübergreifend!). Fehler wird es geben — reines
  Text-Editing ohne Engine-Feedback macht das so gut wie sicher (Tippfehler in Transform-Werten,
  eventuell fehlende Kollisionsebenen, Balancing).

---

## Was bewusst NICHT gebaut wurde (Umfang für diese Runde)

- **Fraktionsquestketten** (Beitritt/Prüfungen/Aufstieg laut
  [Fraktionsquests.md](konzept/Quests/Fraktionsquests.md)) — nur die NPCs stehen in der Welt, mit
  einer Flavor-Dialogzeile. Keine echten Quest-JSONs dafür.
  Nur `quest_der_bote`/`quest_die_nachricht` (Akt 1, Teil 1) sind spielbar.
- "Erste Schritte in der Barriere" (Orientierungsquest aus Haupthandlung.md Akt 1) nicht gebaut.
- Story-Beats aus Akt 2/3 (Ulf-Bosskampf, Vox Aedificares' Entführungen, die fünf Erbauer-Tempel,
  Wandbild-/Katakomben-Erkundung) sind nur Konzept, nicht als Quests/Content umgesetzt.
- Echtes Timing-/Kombosystem (nur simples Links-/Rechts-Abwechseln bisher, siehe Milestone 6),
  keine Freischaltung von Kombos durch Waffentraining, keine Zauber-/Cooldown-Anzeige im HUD,
  kein Shop/Handel-UI (Händler-NPCs wie Godric/Ivo/Curran haben nur Flavor-Dialoge, keine
  Kauf-Funktion), kein Drag&Drop/Slot-Grafik fürs Equip (nur ein Knopf pro Item).
- Keine Kunst/Animation/Sound — alles sind farbige Platzhaltergeometrie (Kapseln/Boxen), passend
  zum bisherigen Projektstand (auch Player/Gerald waren schon vorher nur Kapseln).
- **Nichts davon wurde im Editor gespielt** (kein Godot-Binary verfügbar) — siehe Milestone 5/6.

---

# Phase 2 — Welt, Bevölkerung, Handel, Training (Auftrag vom 2026-07-18, Folgesession) — ABGESCHLOSSEN (Code-/Daten-seitig)

Neuer Auftrag, nachdem Phase 1 (Milestones 1–6 oben) einmal angespielt wurde. Alle 6
Unter-Milestones (7–12) sind jetzt umgesetzt, siehe Details unten. **Wie in Phase 1: nur
`dotnet build`- und Konsistenz-geprüft, nicht im Godot-Editor gespielt** (kein Editor in dieser
Umgebung).

## Milestone 7 — Weltausbau (größere Zonen, Unterbereiche, angedeutete Orte) — ERLEDIGT

- [x] Alle 4 Zonen deutlich vergrößert (Nethora reicht jetzt z.B. von z=14 bis z=-34 statt bis
  -15, ähnlich für die anderen).
- [x] Nethora in Unterbereiche gegliedert: Burg/Schloss (mit Zugangskontrolle, siehe Milestone
  11), Ordenssitz, Marktviertel (4 Stände: Godric/Ivo/Meinhard/Lambert), Taverne, Wohnviertel
  (2 Wohnhäuser: Bauer/Bürgerin/Bettler/Wache), Minen-Zugang als Hinweis-Prop.
- [x] Freies Lager: Lagerhöhle, Trainingsbereich, Altmagier-Bereich, neuer Wachposten.
- [x] Sektentempel: Haupttempel, Ritualkammer, angedeuteter Katakomben-Zugang (Hinweis-Prop,
  nicht begehbar).
- [x] **Angedeutete Orte** in `Ankunft.tscn` (7 Prop-Cluster, keine eigene Zone/Transition):
  Küstenregion (blauer Flächenmarker), Mine von Nethora, Alte Kathedrale (hohe Ruinenspitze),
  4 weitere Erbauer-Tempel (violette Obelisken an den vier Ecken der Zone) — passend zum
  "Pentagramm" aus `Orte.md`.
- [x] Gegner (Wolf/Knochenwächter, jetzt bei z=-22/-24) und neue Wegelagerer (z=-20/-25) so
  platziert, dass der Aggro-Radius nicht mehr sofort am Spawn triggert (Exit zu Nethora jetzt bei
  z=-30, viel Laufweg dazwischen).

## Milestone 8 — NPC-Bevölkerung (benannt + generisch) — ERLEDIGT

- [x] 8 generische Rollen-NPCs angelegt, `name` = Rollenbezeichnung statt Eigenname: Bauer,
  Bürgerin, Bettler, Wache, Bandit, Schmuggler, Anhänger, Novize (`Data/Characters/npc_*_1.json`).
- [x] Generische Flavor-Dialoge dafür (ein `start`-Knoten + Exit-Choice, gleiches Muster wie
  bisherige Flavor-NPCs). **Keine Zufallsauswahl unter mehreren Varianten** — bewusst
  zurückgestellt, war "optional" im ursprünglichen Plan und nicht Teil des expliziten Auftrags.
- [x] 5 neue benannte NPCs: Meinhard (Kräuterpater/Reagenzien-Händler), Lambert (Krämer), Bertram
  (Waffenmeister/Einhand-Lehrer), Ivar (Zweihand/Äxte-Lehrer), Malvern (Sekte, Kampf-Lehrer).
- [x] **Jeder neue Questgeber hat eine echte Quest**: Renward (`quest_wegelagerer`, kill_enemy —
  neuer Objective-Typ, siehe unten), Torvald (`quest_ruinenfragmente`, collect_item), Kael
  (`quest_ritualzutaten`, collect_item), und der generische **Bettler**
  (`quest_bettler_gefallen`, collect_item + reines Gold-Reward) als Nachweis, dass generische
  NPCs genauso Questgeber sein können wie benannte.
- [x] Nebenbei: `QuestObjective`/`QuestManager` um `"kill_enemy"` erweitert (TargetId =
  CharacterId des Gegners, ausgelöst von `Enemy.OnDied`) — war Voraussetzung für
  `quest_wegelagerer`. `QuestDefinition.RewardGold` ergänzt (jetzt, wo Gold existiert, siehe
  Milestone 9).

## Milestone 9 — Handelssystem (Händler) — ERLEDIGT

- [x] `Inventory.Gold` (int, kein Item-Stack) + `AddGold`/`SpendGold`/`RestoreGold`,
  `GoldChanged`-Signal, dauerhafte HUD-Anzeige (`GoldLabel`), persistiert in `SaveData`.
- [x] `ItemDefinition.Price` ergänzt, allen verkäuflichen Items einen Preis gegeben
  (Waffen/Rüstung/Schilde/Verbrauchsgüter/Reagenzien), Questgegenstände bleiben auf 0.
- [x] `CharacterDefinition.ShopItemIds` (1:1 am Charakter, keine eigene `Data/Shops/`-Kategorie).
- [x] Neues `ShopPanel` in `Hud.tscn`/`Hud.cs` (`PanelKind.Shop`): Kaufen-Spalte (Händler-
  Sortiment) + Verkaufen-Spalte (eigenes Inventar, nur Items mit `Price > 0` und
  `Type != "quest"`), Verkaufspreis = halber Kaufpreis.
- [x] `DialogueChoice.OpenShop` + `DialogueRunner.ShopRequested`-Signal (trägt die CharacterId
  des sprechenden NPCs mit, dafür `DialogueRunner.Start()` um `speakerCharacterId` erweitert,
  `Npc.Interact()` gibt jetzt seine `CharacterId` mit).
- [x] Godric, Ivo, Curran (nachgerüstet) + Meinhard, Lambert (neu) haben eine "Handeln"-Choice.

## Milestone 10 — Trainingssystem (Lehrer für Waffen/Kombos) — ERLEDIGT

- [x] Kein separates Trainings-UI gebaut — Lehrer sind einfach Händler (Milestone 9), deren
  Sortiment ein `skill_*`-Pseudo-Item enthält (`skill_onehanded_combo`, `skill_twohanded_combo`,
  `Type: "skill"`). Kauf setzt `GameFlags.SetFlag($"learned_{itemId}")` statt einen
  Inventareintrag zu erzeugen (siehe `Hud.OnBuyPressed`) — deutlich weniger neuer Code als eine
  eigene Trainings-Dialogmechanik, gleiches Kauf-Feedback (Gold-Check, "(gelernt)"-Anzeige) gratis
  mitgenommen.
- [x] `ItemDefinition.WeaponCategory` (onehand|twohand|bow) ergänzt, alle Waffen-Items befüllt.
  `Player.PerformAttack`/`IsComboUnlocked` prüft je nach Kategorie
  `learned_onehanded_combo`/`learned_twohanded_combo` — ohne Flag nur Basisangriffe unabhängig
  vom Klickmuster, mit Flag die volle Links-/Rechts-Combo aus Milestone 6. Bögen: keine
  Combo-Mechanik (ergibt beim reinen Klick-Kampf ohnehin keinen Sinn).
- [x] Trainer verknüpft: Bertram + Hagen (Einhand), Ivar (Zweihand), Malvern (Sekte, Einhand).
- [x] Magie-Lehrer-Gating (Torvald/Ansel/Voss) **nicht gebaut** — war explizit niedrige Priorität
  im Plan, da nicht angefragt.

## Milestone 11 — Proaktive NPC-Interaktion (Ansprache + Zugangskontrolle) — ERLEDIGT

- [x] `scripts/Interaction/ProximityDialogue.cs` + `Objects/Triggers/ProximityDialogue.tscn`:
  Area3D, löst einmalig (per `TriggerFlagId`) automatisch `DialogueRunner.Start(...)` aus.
  2 Beispiel-Platzierungen: Bettler in Nethora, Halvor in Sektentempel (begrüßt Neuankömmlinge,
  passend zu seiner Konzept-Rolle "führt Neuankömmlinge ein").
- [x] `scripts/World/GuardCheckpoint.cs` + `Objects/Triggers/GuardCheckpoint.tscn`: Area3D mit
  Kind-`StaticBody3D` "Barrier" (sichtbare halbtransparente rote Wand + Kollision). Ohne
  `RequiredFlagId` blockiert, öffnet automatisch bei `GameFlags.FlagChanged`. Zusätzlich
  erzwungener Dialog beim Anlaufen. Platziert als `BurgtorWache` in `Nethora.tscn`
  (`RequiredFlagId = quest_completed_quest_der_bote`, Wache = Renward, dessen Dialog jetzt auch
  einen `deny_entry`-Knoten hat).
  **Bekannte Einschränkung**: einzelne 16 Einheiten breite Barriere, kein voller Zaun — auf
  offenem Blockout-Gelände theoretisch umgehbar. Dokumentiert in `doc/architektur.md`.
- [x] Entscheidung zur Überschneidungsfrage: `ProximityDialogue` und `GuardCheckpoint` blieben
  getrennte Klassen — `GuardCheckpoint` braucht zusätzlich die physische Barriere-Logik
  (Flag-Listener, CollisionShape/Mesh toggeln), das hätte `ProximityDialogue` unnötig verkompliziert.

## Milestone 12 — Integration/Polish (Phase 2) — ERLEDIGT

- [x] `dotnet build` sauber nach jedem Schritt (durchgehend eingehalten, wie in Phase 1).
- [x] `.tscn`-Konsistenzprüfung nach jeder Zonenänderung wiederholt (Skript geprüft: keine
  hängenden Ext-/SubResource-Referenzen).
- [x] **Zusätzlich** (über die reine Referenzprüfung hinaus): vollständiger Cross-Check aller
  `CharacterId`/`ItemId`/`DialogueId`/Quest-Referenzen zwischen `Data/*.json` und den `.tscn`-
  Dateien (Questgeber existieren, Loot-/Shop-/Belohnungs-Items existieren, Dialog-Ziele
  existieren) — keine Abweichungen gefunden.
- [x] `doc/anforderungen.md` um "Handel & Training" ergänzt, bestehende Punkte aktualisiert.
- [x] `doc/architektur.md` um "Zonen" (Phase 1 schon vorhanden), "Handel", "Training",
  "Proaktive NPC-Interaktion" ergänzt, "Bewusste Vereinfachungen" erweitert.
- [x] Backlog-Eintrag für diese Phase (siehe `doc/backlog/2026-07-18.md`).
- [ ] Weiterhin kein Godot-Editor verfügbar — **echter Testdurchlauf bleibt der wichtigste
  offene Punkt**, nachdem jetzt eine deutlich größere Fläche an neuem Verhalten (Handel,
  Training, Zugangskontrolle, Ambient-Dialoge, größere Zonen) ungetestet ist.

## Zuletzt / Nächster Schritt

**Stand:** Phase 1 (Milestones 1–6) UND Phase 2 (Milestones 7–12) sind jetzt beide vollständig
code-/daten-seitig umgesetzt. `dotnet build`: 0/0. Automatisierte `.tscn`-Konsistenzprüfung plus
vollständiger Cross-Check aller Character-/Item-/Dialogue-/Quest-Referenzen zwischen `Data/*.json`
und den Zonen-Szenen: keine Abweichungen gefunden. **Nach wie vor kein Godot-Editor in dieser
Umgebung** — nichts davon wurde tatsächlich gespielt.

Gesamtstand des Projekts: 4 deutlich größere, in Unterbereiche gegliederte Zonen; ~19 benannte +
8 generische NPCs; 7 Quests (Akt-1-Kette + 4 Nebenquests über alle drei Fraktionen und einen
generischen NPC); vollständiges Kampf-/Magie-/Handels-/Trainingssystem; Zugangskontrolle am
Nethora-Burgtor; zwei Beispiele für proaktive NPC-Ansprache; sieben angedeutete Orte aus dem
Weltkonzept.

**Nächster Schritt:** Ein echter Editor-Testdurchlauf ist jetzt der mit Abstand wichtigste
offene Punkt — die Menge an ungetestetem, aber vernetztem neuem Verhalten (Zonenübergänge,
Handel, Training, Zugangskontrolle, Quests, Kampf) ist inzwischen beträchtlich. Erst danach ist
weiterer inhaltlicher Ausbau (mehr Fraktionsquests, Akt 2/3, echtes Timing-Kampfsystem) sinnvoll
priorisierbar, da sich Balancing/Fehler erst im echten Spiel zeigen.

---

# Architektur-Pivot — Zonen-Wechsel entfernt, durchgehende offene Welt (Folgesession)

Nutzer hat das Spiel erstmals selbst gespielt und dabei drei echte Probleme gefunden, die reine
Textprüfung nicht finden konnte:

1. **Verwirrung, welche Szene zu starten ist** — `Testlevel.tscn` tauchte wieder auf (Godot hatte
   sie als Editor-Tab offen und beim Speichern neu geschrieben, obwohl die Datei vorher gelöscht
   wurde). Gelöst: Datei erneut entfernt, Tab-Hinweis gegeben.
2. **Absturz beim ersten Zonenübergang** — `ZoneManager.LoadZoneContent` hat `child.Free()`
   (sofort) statt `child.QueueFree()` (verzögert) benutzt, aufgerufen aus dem Signal-Handler
   eines der zu löschenden Kinder selbst (`ZoneTransition.OnBodyEntered`). Klassischer Godot-
   Absturz: Node löscht sich quasi selbst mitten im eigenen Callback. Gefixt (siehe
   `doc/architektur.md` "Lektion: Free() vs. QueueFree()") — **aber dann kam Punkt 3**.
3. **Grundsätzliches Missverständnis der Weltstruktur**: Nutzer wollte gar kein Zonen-Ladesystem
   (wie in Gothic 1), sondern eine **durchgehende offene Welt** — alles soll gleichzeitig geladen
   sein, man läuft selbst hin, sieht alles schon von weitem. Das war nie explizit abgestimmt
   worden (ich hatte "zonenbasiert wie Gothic 1" in `Erkundung.md` selbst als Konzept-Entscheidung
   getroffen, siehe dortige Doku) — auf Nachfrage (`AskUserQuestion`) hat der Nutzer sich für die
   offene Welt entschieden.

## Durchgeführter Umbau

- [x] **Komplett entfernt** (nicht nur deaktiviert): `scripts/World/ZoneManager.cs`,
  `scripts/World/ZoneTransition.cs`, `scripts/World/Main.cs`, `Objects/Triggers/
  ZoneTransition.tscn`, `Level/Main.tscn`, `SaveData.CurrentZoneScenePath` +
  zugehörige `SaveSystem`-Logik, `ZoneManager`-Autoload-Eintrag in `project.godot`.
- [x] Alle 4 Gebiets-Dateien (`Level/Zones/*.tscn`) bereinigt: `ExitToX`-Trigger-Instanzen,
  `SpawnFromX`/`SpawnDefault`-Marker entfernt (kein Zonenwechsel mehr, der sie bräuchte). Auch
  die pro-Gebiet-eigenen `Environment`/`Ground`-Instanzen entfernt (sonst mehrere überlappende
  `WorldEnvironment`-Nodes/Kollisionsebenen in der gemeinsamen Szene).
- [x] Neue `Level/World.tscn` als `run/main_scene`: enthält `Player`, `Hud`, **ein** gemeinsames
  `Environment`+`Ground`, und alle 4 Gebiete als Kindnodes mit Transform-Offset (Ankunft bei
  Ursprung, Nethora bei `(0,0,-70)`, Freies Lager bei `(-90,0,-80)`, Sektentempel bei
  `(90,0,-80)`) — Offsets so gewählt, dass sich keine Gebiets-Bounding-Box überschneidet und
  dazwischen offenes, begehbares Gelände liegt. Kein Bootstrap-Skript mehr nötig (`Player`/`Hud`
  sind einfach normale Kindnodes, keine Laufzeit-Instanziierung).
- [x] `GuardCheckpoint`/`ProximityDialogue` unverändert übernommen (hingen nie von `ZoneManager`
  ab), funktionieren unverändert innerhalb ihres jeweiligen Gebiets-Subtrees.
- [x] `doc/architektur.md`: "Zonen"-Abschnitt zu "Welt" umgeschrieben (Entscheidungsgeschichte +
  neue Struktur), neuer Abschnitt "Lektion: Free() vs. QueueFree()" für die zukünftige
  Vermeidung desselben Fehlers. `doc/anforderungen.md` "Welt/Map" aktualisiert.
- [x] Vollständige Konsistenzprüfung wiederholt: keine hängenden Ext-/SubResource-Referenzen,
  keine verwaisten `ZoneManager`/`ZoneTransition`/`Main.tscn`-Erwähnungen mehr im ganzen Projekt,
  `dotnet build`: 0 Fehler, 0 Warnungen.

## Was das für die Konzept-Doku bedeutet

[Erkundung.md](konzept/Gameplay/Erkundung.md) beschreibt noch "Zonenbasiert statt nahtlos offen,
analog zu Gothic 1" — das ist jetzt **nicht mehr aktuell** und sollte bei nächster Gelegenheit
aktualisiert werden (nicht in dieser Runde gemacht, da der Fokus auf dem funktionierenden Umbau
lag, nicht auf Konzept-Dokupflege). Kurzer Hinweis für die nächste Session: Erkundung.md auf
"durchgehende offene Welt" korrigieren.

## Zuletzt / Nächster Schritt (aktualisiert)

**Stand:** Zonen-Wechsel-System vollständig durch eine einzige zusammenhängende Weltszene
ersetzt. `dotnet build`: 0/0. Automatisierte Konsistenzprüfung + Grep nach verwaisten Referenzen:
sauber. **Weiterhin nie im Editor gelaufen** (kein Godot-Binary hier) — der jetzige Umbau basiert
auf Nutzer-Feedback aus dessen eigenem Editor-Test, nicht auf eigener Beobachtung.
**Nächster Schritt:** Nutzer testet erneut, ob die Welt jetzt durchgehend begehbar ist und alle
vier Gebiete ohne Ruckeln/Absturz erreichbar sind. Falls ja: `Erkundung.md` aktualisieren (siehe
oben) und mit dem eigentlichen inhaltlichen Testdurchlauf (Kampf, Handel, Training, Quests)
weitermachen.
