# Hauptquestreihe

<!--
Quest-für-Quest-Ablauf der Haupthandlung (siehe auch Story/Haupthandlung.md), inkl. wo sich die
Fraktionswahl auswirkt/verzweigt.
-->

Setzt die Akt-Struktur aus [Haupthandlung.md](../Story/Haupthandlung.md) in konkrete
Quest-Schritte um. Vorläufige Namen/Reihenfolge, gut geeignet zum Aufbrechen in einzelne
`Data/Quests/*.json` (siehe `doc/architektur.md`).

## Akt 1 — Ankunft

1. **"Der Bote"** — Ausgangsquest, automatisch aktiv bei Spielstart. Ziel: den Königsbrief (siehe
   [Verbrauchsgueter-Questitems.md](../Items/Verbrauchsgueter-Questitems.md)) nach Nethora bringen.
2. **"Erste Schritte in der Barriere"** — Orientierungsquest, führt an mindestens einem Vertreter
   jeder Fraktion vorbei (z.B. Cormac der Wanderer als neutraler erster Kontakt, siehe
   [NPCs.md](../Charaktere/NPCs.md)), ohne Bindung zu erzwingen.
3. **"Die Nachricht"** — Brief an Ordensmeister Cassian bzw. König Aldemar übergeben. Abschluss
   löst Aldemars Zusammenbruch aus (siehe [Antagonisten.md](../Charaktere/Antagonisten.md)) —
   Wendepunkt zu Akt 2.

## Akt 2 — Bindung & Grabung

4. **Fraktionswahl-Quest** (verzweigt, siehe [Fraktionsquests.md](Fraktionsquests.md) für Details
   je Lager) — der Spieler entscheidet sich für Nethora, Freies Lager oder Erleuchtete (oder
   bleibt vorerst ungebunden).
5. **"Wandbilder"** — erste Lore-Quest, führt zu Ruinen/Katakomben mit Arthur-Darstellungen (siehe
   [Erkundung.md](../Gameplay/Erkundung.md)), unabhängig von Fraktionswahl verfügbar.
6. **"Der gefallene Altmagier"** — nur relevant/erreichbar über das Freie Lager: erste Begegnung
   mit Ulfs Abdriften in die Blutmagie, noch nicht der finale Bosskampf.
7. **"Das fünfte Siegel"** — Erkundung mindestens eines der vier Nebentempel (siehe
   [Orte.md](../Welt/Orte.md)) liefert das erste harte Beweisstück, dass der Erbauer real ist.
8. **"Verschwundene Neue"** — Vox Aedificares beginnt, Sektenmitglieder zu entführen; Auftakt zur
   Eskalation, unabhängig davon ob der Spieler der Sekte angehört oder nicht (er bekommt davon
   Wind, z.B. über Thessal oder über Ermittlungen von außen).

## Akt 3 — Eskalation

9. **"Ulf"** — Bosskampf gegen den gefallenen Altmagier (nur Freies-Lager-Pfad, oder als
   Questzugang für andere Fraktionen über eine externe Ermittlung).
10. **"Das Ritual"** — Vox Aedificares' Entführungen kulminieren in einem offenen
    Erweckungsritual, der Spieler muss eingreifen oder sich entscheiden, es geschehen zu lassen.
11. **"Aldemars letzter Befehl"** — König Aldemars Paranoia erreicht einen Kipppunkt (genaue
    Konsequenz offen, z.B. Befehl zur vollständigen Abriegelung oder Selbstaufgabe Nethoras).

## Finale

12. **Finalquest** — verzweigt endgültig nach gewählter Fraktion/Vorgeschichte in eines der
    Enden aus [Haupthandlung.md](../Story/Haupthandlung.md#finale--mögliche-enden-vorschlag-offen-für-anpassung).
