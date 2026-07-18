# Waffen

<!--
Konkrete Waffenliste pro Kategorie (Einhand-/Zweihand-Schwert, Stumpfwaffen, Äxte, Bögen,
Schilde): Name, Fraktion/Herkunft, Schadensart, Werte, Beschaffung (Loot/Händler/Quest-Belohnung).
Landet am Ende als JSON in Data/Items/, siehe doc/architektur.md (Feld scalingStat: strength für
Schwerter, dexterity für Bögen).
-->

Kategorien/Schadensarten siehe [Kampfsystem.md](../Gameplay/Kampfsystem.md). Optik/Materialsprache
je Fraktion siehe [Visueller-Stil.md](../Praesentation/Visueller-Stil.md) (Nethora: Gold/Weiß,
verziert; Freies Lager: grau/silber, geflickt/improvisiert; Erleuchtete: violett/rot, rituell).
Herkunfts-Händler siehe [NPCs.md](../Charaktere/NPCs.md) (Godric, Curran, Wrathe).

## Nethora (Godric — Waffenschmied)

| Name | Kategorie | Schadensart | Beschaffung |
|---|---|---|---|
| Ordensklinge | Einhand-Schwert | Schneid | Standardausrüstung ab Rang Ritter |
| Zeremonienschwert | Zweihand-Schwert | Schneid | Kauf bei Godric, teuer |
| Wachtkeule | Stumpfwaffe | Stoß | Startausrüstung Vigil |
| Custodes-Streitaxt | Axt | Misch | Kauf bei Godric |
| Grenzwächter-Bogen | Bogen | Fernkampf | Loot/Quest-Belohnung Hadrian |
| Ordensschild | Schild | — | Kauf bei Ivo (siehe [Ruestung-Schilde.md](Ruestung-Schilde.md)) |

## Freies Lager (Curran — geflickte Waffen)

| Name | Kategorie | Schadensart | Beschaffung |
|---|---|---|---|
| Geflickter Säbel | Einhand-Schwert | Schneid | Kauf bei Curran |
| Grobe Zweihandklinge | Zweihand-Schwert | Schneid | Loot, selten |
| Söldnerkeule | Stumpfwaffe | Stoß | Startausrüstung Söldner |
| Rostige Axt | Axt | Misch | Quest-Belohnung "Kräuter für Gerald" (`sword_rusty`
  vorhandene Testdaten — ggf. hier oder unter Einhand einsortieren, siehe unten) |
| Jagdbogen | Bogen | Fernkampf | Kauf bei Curran, auch bei Roswin (unabhängig) |
| Behelfsschild | Schild | — | Kauf bei Curran |

## Die Erleuchteten (Wrathe — geweihte Klingen)

| Name | Kategorie | Schadensart | Beschaffung |
|---|---|---|---|
| Opferdolch-Schwert | Einhand-Schwert | Schneid | Kauf bei Wrathe |
| Ritualaxt | Axt | Misch | Kauf bei Wrathe, Beschützer-exklusiv |
| Beschützerkeule | Stumpfwaffe | Stoß | Startausrüstung Beschützer |
| Erweckerbogen | Bogen | Fernkampf | Quest-Belohnung Kael |

## Unabhängig

| Name | Kategorie | Schadensart | Beschaffung |
|---|---|---|---|
| Jägerbogen | Bogen | Fernkampf | Kauf/Training bei Roswin oder Osric |
| Wanderstab | Stumpfwaffe | Stoß | Loot in der Wildnis |

## Hinweis zu bestehenden Testdaten

Aktuell existiert bereits `sword_rusty` als Belohnungs-Item der Testquest (siehe
`Data/Items/`, `doc/backlog/2026-07-18.md`). Beim Übertrag in echte Items entweder als eigenes,
schwaches Einstiegs-Einhandschwert beibehalten (fraktionslos, "irgendwo gefunden") oder ins
Freies-Lager-Sortiment integrieren — beides passt zum improvisierten Look.
