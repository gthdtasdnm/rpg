# Kampfsystem

<!--
Ziel laut Notizen: Timing- und kombinationsbasiert wie in Gothic. Waffenarten (Einhand-/
Zweihand-Schwert, Stumpfwaffen, Äxte, Bogen, Schild) und ihre Schadensarten (Schneid-/Stoß-/
Mischschaden), welcher Gegnertyp braucht welche Waffe. Blocken mit/ohne Schild, Stagger-Regeln.
Ausgangsbasis: doc/informationen/Spielmechaniken.txt. Technische Anknüpfung: Strength/Dexterity
in scripts/Characters/CharacterStats.cs, Item.ScalingStat in Data/Items/*.json.
-->

## Grundprinzip

Timing- und kombinationsbasiert statt reines Button-Mashing (Vorbild: Gothic-Nahkampf). Angriffe
haben spürbare Vor-/Nachlaufzeiten, in denen der Charakter festgelegt ist — Ausweichen/Blocken
muss vorausschauend getimt werden, nicht reaktiv aus jeder Position möglich sein.

## Waffenarten & Schadenstypen

| Kategorie | Beispiele | Schadensart | Skaliert mit |
|---|---|---|---|
| Einhand-Schwert | siehe [Waffen.md](../Items/Waffen.md) | Schneidschaden | `strength` |
| Zweihand-Schwert | " | Schneidschaden (höher, langsamer) | `strength` |
| Stumpfwaffen (Keule, Morgenstern) | " | Stoßschaden | `strength` |
| Äxte | " | Misch aus Schneid- und Stoßschaden | `strength` |
| Bogen | " | Fernkampf, eigener Schadenswert | `dexterity` |
| Schild | — kein eigener Schaden | — | (Verteidigung) |

Gegner haben unterschiedliche Resistenzen/Schwächen gegen Schneid- vs. Stoßschaden (Vorschlag,
passend zum "welcher Gegnertyp braucht welche Waffe"-Ziel): ungepanzerte/organische Gegner
(mutierte Tiere) sind anfälliger für Schneidschaden, gepanzerte/knochige Gegner (Untote,
gerüstete Sektenwächter) anfälliger für Stoßschaden. Äxte als Mischwaffe sind dadurch die
generalistische, aber nie optimale Wahl — echte Spezialisierung lohnt sich gegnerabhängig.

## Blocken & Stagger

- **Mit Schild**: aktives Blocken reduziert eingehenden Schaden stark, geringes
  Stagger-Risiko für den Blockenden.
- **Ohne Schild** (bloße Waffe): Blocken ist möglich, aber schwächer — der Block wird leichter
  durchbrochen (höheres Stagger-Risiko) und ein Teil des Schadens kommt trotzdem durch.
- **Stagger**: ein gestaggerter Gegner (oder Spieler) ist kurz handlungsunfähig/aus dem Timing
  geworfen — offenes Zeitfenster für einen Konterangriff. Zweihandwaffen und Stumpfwaffen staggern
  stärker als leichte Einhandwaffen, dafür langsamer im Ausholen.

## Offene Punkte

- Genaue Zahlenwerte (Schadens-/Stagger-Werte) folgen erst mit Gegnern/`scripts/Combat/`
  (aktuell laut [`doc/anforderungen.md`](../../anforderungen.md) noch nicht umgesetzt).
- Wie Magie (siehe [Magiesystem.md](Magiesystem.md)) in den Nahkampf-Loop eingreift (z.B.
  Unterbrechbarkeit während göttlicher Kanalzeit durch gegnerische Treffer).
