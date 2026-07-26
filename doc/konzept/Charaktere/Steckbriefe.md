# Steckbriefe

Wer ist die Person, wie steht sie zur Welt, **wie redet sie**. Ein Eintrag pro Charakter, der
wirklich gebaut wird (Dialog + `Data/Characters/<id>.json`).

Zweck: Dialoge bleiben über Monate hinweg konsistent. Wenn Halvor in Akt 3 wieder auftaucht, muss
er klingen wie in Akt 1 — dafür ist der Abschnitt **Sprechweise** da, nicht die Biografie.

Abgrenzung: [NPCs.md](NPCs.md) ist die *Liste* aller geplanten Figuren (Name, Rang, ein Satz).
Hier stehen nur die **ausgearbeiteten** — dafür ausführlich.

---

## Vorlage (kopieren)

```markdown
## <Name>

**Kurz**: <ein Satz — wer ist das für den Spieler>
**Datei**: `Data/Characters/npc_<id>.json` · `Dialogues/<name>.dialogue`
**Fraktion / Rang**: <…>
**Rolle im Spiel**: <Questgeber / Händler / Trainer / Lore / …>

**Haltung**: <wozu steht er, woran glaubt er, wem ist er treu>
**Charakter**: <wie tritt er auf>
**Sprechweise**: <Satzlänge, Wortwahl, Anrede, was er NIE sagen würde>
**Weiß nicht**: <was ihm verborgen ist — wichtig, damit er nicht zu viel verrät>
**Verhältnis zu**: <andere Fraktionen/Figuren>
```

---

## Halvor

**Kurz**: Der erste Mensch, den der Spieler in der Barriere trifft. Ruppiger Ordensmann, der
Neuankömmlinge einsammelt, bevor sie draußen umkommen.
**Datei**: `Data/Characters/npc_halvor.json` · `Dialogues/halvor.dialogue`
**Fraktion / Rang**: Nethora — Orden der Custodes, **Vigil** (Anwärter)
**Rolle im Spiel**: Auftakt-NPC. Erklärt Welt, Barriere und die drei Lager, gibt
`quest_nach_nethora` (in die Burg kommen).

**Haltung**: Königstreu, ohne darüber nachzudenken. Für ihn ist Lord Aldemar "das Einzige, was
zwischen uns und dem absoluten Chaos steht" — Ordnung ist kein Ideal, sondern Überlebensfrage.
Glaubt fest daran, dass der König draußen Nethora eines Tages wieder herausholt. Das ist keine
Hoffnung, das ist für ihn eine Tatsache.

**Charakter**: Seit elf Jahren in der Barriere und Vigil. Er ist nett etwas älter und wird für den helden eine art mentor.
Er hat ein gutes herz aber die lange zeit hat an seinem charakter spuren hinterlassen.
er ist nicht blöd und lässt sich nicht ausnutzen.
Herablassend gegenüber Neuankömmlingen ("eine arme Sau"), gleichzeitig aber der Einzige, der sich
überhaupt die Mühe macht. Diese Spannung ist sein Kern: **rauer Ton, tatsächliche Hilfe**.

**Sprechweise**: Kurze Sätze, Umgangssprache, kein Sie, keine Höflichkeitsfloskeln. Sarkasmus statt
Drohung ("Außer, du hast Lust, hier draußen gefressen zu werden"). Nennt Dinge beim Namen,
beschönigt nichts. Redet über andere Fraktionen mit offener Verachtung, nie neutral.
**Sagt nie**: gewählte oder fromme Formulierungen, Gebetsfloskeln, lange Erklärungen. Wenn er
etwas nicht weiß, sagt er "Frag mich nicht, warum" statt zu spekulieren.

**Weiß nicht**: Dass der Erbauer real ist. Für ihn ist "das Übel in der Tiefe" alte Ordenstradition,
über die man nicht nachdenkt. Er weiß auch nichts vom Inhalt des Königsbriefs.

**Verhältnis zu**: *Freies Lager* — "dreckiges Pack von Verrätern", Wilde ohne Ehre.
*Die Erleuchteten* — kein Lager, sondern "Sektenspinner", Heiden, gefährlich.
*Orden/König* — bedingungslose Loyalität, auch wenn ihn der Orden selbst nie befördert hat.

**Offen**: Was hat ihn vor elf Jahren hier reingebracht? (Er ist selbst Verurteilter — das erklärt,
warum er es nie über den Vigil-Rang hinaus schafft. Noch nicht ausgespielt.)

---

## Falk & Knud

Die beiden gehören zusammen und werden immer **als Paar** gespielt — deshalb ein gemeinsamer
Steckbrief. Sie stehen draußen an der Jägerhütte und fangen ab, wer neu durch die Barriere kommt,
bevor Nethora ihn kriegt.

**Dateien**: `Data/Characters/npc_falk.json` · `Dialogues/falk.dialogue`
und `Data/Characters/npc_knud.json` · `Dialogues/knud.dialogue`
**Fraktion**: Freies Lager — inoffizielle Werber, kein Rang, keine Beauftragung
**Rolle im Spiel**: Erster Kontakt zum Freien Lager. **Lehrer**: Falk → `skill_bogen`,
Knud → `skill_haeuten` (je 100 Silber). Erklären die Jagd.

**Die Dynamik**: Falk redet, Knud arbeitet. Falk ist der Gefährlichere von beiden — schnell,
spöttisch, testet sofort aus, wie weit er gehen kann. Knud ist der Schwerere: langsamer, weniger
Worte, lacht an den falschen Stellen. Der Witz an ihnen ist, dass Knud unter der Grobheit
tatsächlich etwas Brauchbares sagt (siehe unten), Falk dagegen nie.

**Haltung**: Kein Idealismus. Sie glauben nicht an die Sache des Freien Lagers, sie finden sie nur
bequemer als knien und beten. Ihre Werbung ist ehrlich in der Sache und verlogen im Ton: „Bei uns
fragt keiner, was du vorher warst" stimmt — sie meinen es nur nicht nett.

**Charakter**: Raubtiere, keine Schläger. Sie greifen den Spieler nicht an, sie **nutzen** ihn aus.
Der Bier-Handel ist der Kern der Figur: Falk verspricht eine Waffe für ein Bier, nimmt das Bier und
weiß von nichts. Das ist keine Nebenszene, das ist ihre Visitenkarte.

**Sprechweise**: Kurz, schneidend, viele Befehle. Duzen von oben herab („Freundchen", „kluger
Junge"). Falk droht nie direkt, er deutet an („bevor ich es mir anders überlege"). Knud spricht in
Feststellungen ohne Höflichkeit — „Ja." als ganze Antwort ist typisch für ihn.
**Sagen nie**: Entschuldigungen, Dank, Frömmigkeit. Falk gibt niemals einen Fehler zu.

**Wissen sie nicht**: Nichts über den Erbauer oder das Siegel. Sie sind einfache Leute mit einem
Bogen, keine Altmagier.

**Wichtig fürs Weiterschreiben**: Knud ist der Einzige, der dem Spieler **nach** dem Betrug erklärt,
was gerade passiert ist („Das war die billigste Lehrstunde, die du hier kriegen wirst"). Das ist
bewusst so — es hält die beiden davon ab, reine Karikaturen zu sein, und macht Knud zur Figur, die
man später wiedersehen kann. Falk nicht.

**Verhältnis zu**: *Nethora* — Verachtung („knie vor ihrem König, bete dreimal am Tag").
*Halvor* — kennen ihn vom Sehen, halten ihn für einen Speichellecker. Umgekehrt gilt dasselbe.
*Untereinander* — jahrelang eingespielt, Falk führt.

> **Offen**: Ob die beiden im Lager selbst wieder auftauchen oder nur hier draußen vorkommen.
> Bisher stehen sie in keiner Lager-Rangliste (siehe [NPCs.md](NPCs.md)).
