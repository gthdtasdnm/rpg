# NPCs

<!--
Liste wichtiger NPCs: Name, Fraktion, Rolle/Funktion (Questgeber, Händler, Trainer, ...),
Kurzcharakterisierung. Technisch landet jeder NPC am Ende als JSON in Data/Characters/, siehe
doc/architektur.md.
-->

Rangsystem siehe [Fraktionen.md](../Welt/Fraktionen.md). König Aldemar, Der Erbauer und
Vox Aedificares sind bereits unter [Antagonisten.md](Antagonisten.md) beschrieben und hier nur
verlinkt, nicht doppelt aufgeführt. Namen sind Vorschläge (angelehnt an Gothic 1 in Rollenwahl,
aber eigene Namen) — gerne anpassen.

Namen/Rollen sind noch ohne Zahlen/Werte, das kommt erst, wenn eine Figur wirklich als JSON in
`Data/Characters/` angelegt wird.

## Nethora (Orden der Custodes Profundi + Stadt)

| Rang | Name | Kurztext |
|---|---|---|
| Ordensmeister | Cassian | Oberhaupt der Custodes Profundi. Kennt die ältesten Archivtexte des Ordens, glaubt selbst kaum noch an ihre wörtliche Bedeutung. |
| Paladin (Hauptmann) | Renward | Hauptmann der Stadtwache. Misstrauisch gegenüber dem Spieler, da dieser als Verbrecher in die Barriere geschickt wurde. |
| Paladin | Gerhard | Ausbilder für angehende Paladine. Hart, aber fair — prüft jeden Anwärter persönlich im Zweikampf. |
| Paladin | Hadrian | Wache am äußeren Grenzbereich der Barriere. Wortkarg, immer in Alarmbereitschaft. |
| Paladin | Sebastian | Elite-Wächter im innersten Bereich des Ordens. Sehr fromm, spricht oft in Zitaten. |
| Ritter | Dietrich | Führt eine Streife gegen Wegelagerer. Questgeber für erste Kampfaufträge. |
| Ritter | Konrad | Frisch befördert, ehrgeizig. Sieht den Spieler eher als Konkurrenz denn als Verbündeten. |
| Ritter | Bertram | Waffenmeister des Ordens. Allgemeiner Kampflehrer für den Aufstieg Vigil → Ritter. |
| Ritter | Roderich | Nimmt gerne an Turnieren teil. Bietet eine Wettkampf-Nebenquest an. |
| Vigil | Oswin | Anwärter, schüchtern. Bittet um Hilfe bei seiner Bewährungsaufgabe. |
| Vigil | Tobias | Jüngster Vigil im Orden, wird von älteren Rittern schikaniert. |
| Vigil | Fabian | Torwache. Kleine Quest um einen verlorenen Gegenstand. |
| Priester | Ansel | Lehrer für göttliche Magie, hält Gebete auf Latein. Ruhig, geduldig, unerschütterlich loyal zum König. |
| Priester | Ignaz | Archivar der Kirche. Hütet alte, kaum noch gelesene Texte über "ein Übel in der Tiefe" — wichtiger Lore-Questgeber. |
| Priester | Ulrich | Segnet Ausrüstung von Reisenden. Questgeber für Beschaffung geweihter Gegenstände. |
| Mönch | Wendelin | Lehrmeister für den Übergang Ritter → Mönch. Erste Stufe der Kontemplation und Gebete. |
| Mönch | Leopold | Kopiert Manuskripte in der Klosterbibliothek. Introvertiert, kennt viele Gerüchte. |
| Händler (Waffen) | Godric | Waffenschmied, beliefert den Orden mit Schwertern, Äxten und Stumpfwaffen. |
| Händler (Rüstung/Schilde) | Ivo | Rüstmeister. Fertigt und verkauft Rüstungen und Schilde, getrennt vom Waffenhandel. |
| Händler (Magie/Reagenzien) | Meinhard | Kräuterpater. Verkauft Reagenzien und Schriftrollen für göttliche Magie sowie Heiltränke. |
| Händler (Kram/Allgemein) | Lambert | Krämer. Werkzeug, Proviant, Alltagskram — keine Waffen, keine Magie. |
| Wirt | Hartwin | Betreibt die Taverne. Gute Quelle für Gerüchte und Questhinweise. |

## Freies Lager

| Rang | Name | Kurztext |
|---|---|---|
| Klingenbruder (informeller Anführer) | Grimbold | Aktuell "erster unter Gleichen" im Lager — führt durch Ansehen, nicht durch Titel. |
| Söldner | Bram | Sucht Verstärkung für einen gefährlichen Auftrag. Questgeber. |
| Söldner | Falk | Wachdienst rund ums Lager. Ruppig, aber loyal zu den Seinen. |
| Söldner | Zane | Fordert Neuankömmlinge gern zu einem Schaukampf heraus. |
| Klingenbruder | Hagen | Veteran. Allgemeiner Kampflehrer des Lagers. |
| Klingenbruder | Ivar | Spezialist für Zweihandwaffen und Äxte. Fraktionsspezieller Kampflehrer. |
| Dieb | Piet | Will, dass etwas Bestimmtes aus Nethora gestohlen wird. Questgeber. |
| Dieb | Ragnar | Konkurriert mit Piet um dieselben "Aufträge". |
| Dieb | Brandt | Gerät ständig in Schwierigkeiten. Comic-Relief-Figur mit kleinen Bittstellungen. |
| Schatten | Sten | Meisterdieb. Lehrt Schleichen und Schlösser knacken. |
| Neuling | Jonas | Frisch im Lager angekommen, ängstlich. Kleine Einstiegsquest. |
| Neuling | Marek | Will beweisen, dass er kein Weichei ist — Prügel-Nebenquest. |
| Lehrling | Kaspar | Erster Kontakt zur alten Magie, neugierig und unsicher zugleich. |
| Lehrling | Nils | Sammelt Ruinenfragmente für den Wissenssucher. |
| Wissenssucher | Otbert | Durchforstet Ruinen nach altem Wissen. Questgeber für Artefakt-/Textbeschaffung. |
| Wissenssucher | Vance | Rivalisiert mit Otbert um dieselben Funde. |
| Altmagier | Torvald | Lehrt die alte Magie. Magie-Lehrer des Lagers. |
| Altmagier | Ulf | Geht der alten Magie zu weit auf den Grund — beginnt sich zu verändern. Siehe [Antagonisten.md](Antagonisten.md). |
| Händler (Waffen) | Curran | Verkauft und repariert improvisierte, geflickte Waffen. |
| Händler (Magie/Schmuggelware) | Doyle | Schwarzhändler für verbotene Reagenzien und Diebesgut. |
| Händler (Kram/Allgemein) | Warrick | Proviant, Werkzeug, Ausrüstung fürs Lagerleben. |
| Wirt | Yorick | Betreibt die improvisierte Taverne im Lager. Gerüchtequelle. |

## Die Erleuchteten (Sekte)

| Rang | Name | Kurztext |
|---|---|---|
| Seher (Hochpriester) | Vox Aedificares | Titel, kein Eigenname — der jeweils ranghöchste Seher, "die Stimme des Erbauers". Siehe [Antagonisten.md](Antagonisten.md). |
| Seher | Mordric | Treuester Gefolgsmann von Vox Aedificares, leitet Rituale mit. |
| Seher | Thessal | Zweifelt insgeheim am eingeschlagenen Weg — möglicher Ansatzpunkt für den Spieler. |
| Seher | Zorath | Erblindet, "sieht" nach eigenen Worten nur noch Visionen. |
| Erwecker | Kael | Leitet ein Erweckungsritual. Questgeber für Ritualzutaten. |
| Erwecker | Voss | Lehrt die Erweckungsrituale. Magie-Lehrer der Sekte. |
| Erwecker | Silas | Fanatisch, misstrauisch gegenüber Fremden. |
| Erwecker | Ombra | Genannt "der Flüsterer" — behauptet ebenfalls, die Stimme zu hören. Rivale um Deutungshoheit mit Vox Aedificares. |
| Erwecker | Amsel | Kräuterkundiger. Mischt Rauschmittel für Rituale und Visionen. |
| Beschützer | Corvin | Bewacht den Zugang zum inneren Kultgebiet. |
| Beschützer | Malvern | Lehrt Kampf im Dienst des Erbauers. Kampf-Lehrer der Sekte. |
| Beschützer | Reth | Jagt einen vermuteten Spion aus Nethora. Questgeber. |
| Beschützer | Nym | Jung, fanatisch. Stellt dem Spieler eine gefährliche Mutprobe. |
| Beschützer | Rennik | Wachthabend an der Außengrenze zum Sektengebiet. |
| Neue | Emeric | Frisch bekehrter Ex-Bürger Nethoras. Zeigt, wie gewöhnliche Menschen in den Kult hineingezogen werden. |
| Neue | Fenwick | Zweifelt bereits wieder an seiner Entscheidung — Ansatzpunkt für eine Ausstiegs-Quest. |
| Neue | Grael | Stellt Neuankömmlingen (auch dem Spieler) eine Bewährungsaufgabe für die Aufnahme. |
| Neue | Halvor | Führt Neuankömmlinge ein, erklärt die Regeln des Kults. |
| Neue | Vael | Arbeitete früher als Bediensteter im Schloss — kennt Gerüchte über den König. Info-Questgeber. |
| Händler (Magie/Reagenzien) | Skarn | Verkauft Ritualzutaten und Opfergaben-Material. |
| Händler (Waffen) | Wrathe | Fertigt geweihte, improvisierte Klingen für die Beschützer. |
| Händler (Kram/Allgemein) | Dorian | Tarnhandel mit Alltagsgütern — auch für Außenstehende zugänglich, ohne dass der Kult auffällt. |

## Unabhängige / Verstreute Charaktere

Gehören keiner Fraktion an, über die Welt verteilt (Wälder, Wege, Ruinen) statt in einem der drei
Lager. Bei manchen reicht der Beruf als Grund, in der Wildnis zu sein (Jäger, Wanderer,
Fährmann, ...); bei anderen — vor allem denen, die aus Nethora selbst kommen — gibt es einen
eigenen Grund, warum sie nicht mehr in der Burg/Stadt sind.

| Rolle | Name | Kurztext |
|---|---|---|
| Jäger | Roswin | Lebt vom Wild der Wälder, verkauft Pelze und Fleisch. Möglicher unabhängiger Lehrer für den Bogen. |
| Wanderer | Cormac | Zieht zwischen den drei Lagern hin und her, verkauft Neuigkeiten und Kleinkram. Allen Fraktionen gegenüber neutral. |
| Waldläufer | Osric | Fährtenleser und Späher, hilft gelegentlich Nethora gegen Bezahlung, ohne dem Orden anzugehören. Unabhängiger Lehrer für Geschicklichkeit/Bogen. |
| Fährmann | Aldous | Kontrolliert eine Flussquerung und verlangt Wegzoll. Da alle Reisenden an ihm vorbeimüssen, kennt er die meisten Gerüchte der Gegend. |
| Schatzsucher | Emrich | Unabhängiger Abenteurer auf der Suche nach alten Ruinen und Artefakten — gerät dabei manchmal in Konkurrenz mit den Wissenssuchern des Freien Lagers. |
| Totengräber/Plünderer | Sarge | Durchsucht verlassene Orte, Schlachtfelder und Ruinen nach Wertgegenständen. Moralisch fragwürdig, aber nützlicher Händler für seltene Fundstücke. |
| Hexer | Morwen | Praktiziert altes Volkswissen — Kräuter, Flüche, Segen. In Nethora wird so etwas als Aberglaube/Hexerei misstrauisch beäugt, deshalb lebt er lieber draußen, wo ihn niemand behelligt. |
| Eremit | Galen | Ehemaliger Mönch des Ordens, der dem Klosterleben entsagte, weil ihm die Rituale zu leer und bedeutungslos wurden. Weiß überraschend viel über alte Kirchentexte. |
| Deserteur | Kilian | Ehemaliger Ritter der Custodes Profundi. Desertierte, nachdem er einen Befehl verweigerte, den er für ungerecht hielt — wird von Nethora gesucht und versteckt sich seither. |
| Geächteter | Hollis | Vor Jahren aus Nethora verbannt — ein ähnliches Schicksal wie das des Spielers, nur länger her. Lebt seither als Einzelgänger, bitter gegenüber der Stadt. |
| Geflohener Palastdiener | Berthold | Floh aus dem Schloss, nachdem er miterlebte, wie der König nach Erhalt der Botschaft durchdrehte. Anders als Vael (der sich den Erleuchteten anschloss) will er mit niemandem mehr etwas zu tun haben. |
| Wilderer | Brack | Jagt illegal in Nethoras Wäldern und versteckt sich vor der Ordenswache. Verkauft heimlich Fleisch und Pelze unter der Hand. |
| Kriegsversehrter | Edric | Ehemaliger Soldat Nethoras, im Kampf verstümmelt und von der Stadt danach fallengelassen. Lebt bitter am Rand der Wildnis. |
| Flüchtling | Thane | Floh mit seinem Sohn aus der zunehmend verarmten Stadt auf der Suche nach einem besseren Leben. Schlägt sich seither in der Wildnis durch. |
