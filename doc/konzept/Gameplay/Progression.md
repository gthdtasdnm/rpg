# Progression

<!--
Wie wird der Charakter stärker (Level, Attributspunkte, Skilltrees, Waffen-/Magie-Training bei
NPCs wie in Gothic)? Wie hängt das mit den Fraktionen zusammen (fraktionsexklusive Fähigkeiten)?
Wie schnell soll der Fortschritt sich anfühlen ("unendlich Kapazität" laut Notizen — heißt das:
kein Inventar-/Levelcap)?
-->

## Attribute statt Skilltree

Kein klassischer Skilltree-Screen, sondern Gothic-Stil: Attribute (`Health`, `Strength`,
`Dexterity`, siehe `scripts/Characters/CharacterStats.cs`) werden durch Erfahrung/Lernpunkte
gesteigert, neue Fähigkeiten/Waffenkategorien werden **bei NPC-Lehrern gegen Bezahlung/Gefallen
gelernt**, nicht automatisch per Levelaufstieg freigeschaltet. Das deckt sich mit den bereits in
[NPCs.md](../Charaktere/NPCs.md) angelegten Lehrer-Rollen:

- **Waffentraining allgemein**: Bertram (Nethora), Hagen (Freies Lager), Malvern (Erleuchtete).
- **Fraktionsspezielles Waffentraining**: Ivar (Freies Lager, Zweihand/Äxte).
- **Unabhängiges Bogentraining**: Roswin oder Osric, außerhalb jeder Fraktion.
- **Magie-Training**: Torvald (alte Magie), Ansel/Wendelin (göttliche Magie/Gebete), Voss
  (Erweckungsrituale) — siehe [Magiesystem.md](Magiesystem.md).
- **Schleichen/Schlösser**: Sten (Freies Lager, Schatten-Zweig).

## Fraktionsexklusive Fähigkeiten

Der Kampf-/Magie-Split existiert in allen drei Fraktionen (siehe
[Fraktionen.md](../Welt/Fraktionen.md)), aber die *Art* der Fähigkeit ist exklusiv:

- Nur das Freie Lager lehrt Diebstahl/Schleichen (Dieb → Schatten) und alte Magie/Blutmagie.
- Nur Nethora lehrt formale göttliche Magie/Gebete in voller Tiefe (Mönch → Priester).
- Nur die Erleuchteten lehren Erweckungsrituale.

Ein Fraktionswechsel oder Bindungen an mehrere Fraktionen gleichzeitig (falls das Spiel das
zulässt) würde dadurch automatisch begrenzt, was der Spieler lernen kann — kein Grinden aller
Fähigkeiten in einem Durchgang, sondern eine echte Entscheidung, siehe
[Themen-Ton.md](../Story/Themen-Ton.md) ("keine reine Wahl").

## Tempo

Fortschritt soll sich, wie in Gothic, langsam und spürbar anfühlen — jeder Attributspunkt/jede
neue Fähigkeit ist eine bewusste Investition, kein beiläufiger Levelaufstieg. Kein hartes
Levelcap vorgesehen.

## "Unendliche Kapazität"

Bezieht sich laut Ausgangsnotiz auf das **Inventar**, nicht auf Attribute/Level: kein
Gewichtslimit, kein Slot-Cap — der Spieler kann uneingeschränkt sammeln. Deckt sich mit der
aktuellen `Inventory`-Implementierung (keine Kapazitätsprüfung). Bewusste Design-Entscheidung
gegen Inventar-Management als Spielmechanik, im Gegensatz zu manchen anderen Survival-/RPG-
Systemen.
