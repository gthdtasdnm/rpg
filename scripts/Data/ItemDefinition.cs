namespace RPG.Data;

// Type: "weapon" | "armor" | "shield" | "consumable" | "material" | "quest" | "skill"
//   ("skill" ist kein echtes Inventar-Item, sondern ein per Shop-UI "kaufbarer" Trainings-Eintrag,
//   siehe Equipment/Hud-Kommentare und doc/spielsysteme.md, Abschnitt 5 - kauft man ihn, wird statt eines
//   Inventareintrags ein GameFlags-Flag `learned_<id>` gesetzt.)
// ScalingStat (nur bei Waffen relevant): "strength" (Nahkampf) | "dexterity" (Bögen)
// DamageType (nur bei Waffen relevant, siehe doc/spielsysteme.md):
//   "slashing" (Schneid) | "blunt" (Stoß) | "mixed" (Axt-Mischschaden) | "ranged" (Bogen)
// WeaponCategory (nur bei Waffen relevant): "onehand" | "twohand" | "bow" - steuert, welches
// Trainings-Flag noetig ist, damit die Combo aus Player.cs greift (siehe SpellCaster-Analogie).
public class ItemDefinition
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Type { get; set; } = "material";

	// Fliesstext fuers Inventar-Detailfeld. Ton wie doc/welt.md: knapp, duester, konkret.
	public string Description { get; set; } = "";

	public string? ScalingStat { get; set; }
	public int Damage { get; set; } = 0;
	public string? DamageType { get; set; }
	public string? WeaponCategory { get; set; }
	public bool Stackable { get; set; } = true;

	// Rüstung/Schild (siehe doc/spielsysteme.md)
	public int Defense { get; set; } = 0;
	public int StaggerResist { get; set; } = 0;

	// Verbrauchsgüter (siehe doc/spielsysteme.md)
	public int HealAmount { get; set; } = 0;

	// Handel (siehe doc/spielsysteme.md, Abschnitt 5): Kaufpreis beim Händler, Verkaufspreis ist die
	// Haelfte (siehe Hud.cs). 0 = nicht handelbar (z.B. Questgegenstaende).
	public int Price { get; set; } = 0;

	// --- Darstellung -------------------------------------------------------------------------
	// EIN Modell fuer alles: es liegt in der Welt (Objects/world_item.tscn), haengt beim Ausruesten
	// am Spieler (EquipmentVisuals) und wird fuer das Inventar-Icon abfotografiert (ItemIcons).
	// Leer = kein Modell; dann zeigt das Inventar ein Namenskuerzel und das Weltitem einen Wuerfel.
	public string Model { get; set; } = "";

	// Skaliert das Modell ueberall gleich. Die Waffenpacks sind bereits metrisch, deshalb 1.0 -
	// noetig nur fuer Packs mit abweichender Einheit oder wenn ein Item bewusst groesser sein soll.
	public float ModelScale { get; set; } = 1f;

	// Längliche Sachen (Schwerter, Stäbe, Bögen) werden fürs Icon automatisch so gedreht, dass sie
	// diagonal im Feld liegen und man ihre breite Seite sieht - die Modellpakete legen ihre Waffen
	// nämlich nicht alle auf dieselbe Achse. Nur ausschalten, wenn die Automatik danebenliegt.
	public bool IconAutoOrient { get; set; } = true;

	// Zusätzliche Drehung fuers Icon (Grad), wird auf die automatische Ausrichtung obendrauf
	// gelegt. Bei kompakten Sachen (Trank, Buch, Schild) greift die Automatik nicht, dann ist das
	// hier die einzige Drehung - Yaw 20 heisst: leicht aus der Frontalen gekippt.
	public float IconYaw { get; set; } = 20f;
	public float IconPitch { get; set; } = 0f;
	public float IconRoll { get; set; } = 0f;

	// >1 zoomt naeher heran, <1 laesst mehr Luft. Die Rahmung passt sonst automatisch.
	public float IconZoom { get; set; } = 1f;

	// Optionales fertiges Bild. Ist es gesetzt, wird das Modell fuer das Icon nicht gerendert.
	public string Icon { get; set; } = "";

	public bool IsEquippable => Type is "weapon" or "shield" or "armor";
}
