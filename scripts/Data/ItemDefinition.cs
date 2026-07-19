namespace RPG.Data;

// Type: "weapon" | "armor" | "shield" | "consumable" | "material" | "quest" | "skill"
//   ("skill" ist kein echtes Inventar-Item, sondern ein per Shop-UI "kaufbarer" Trainings-Eintrag,
//   siehe Equipment/Hud-Kommentare und doc/TODO.md Milestone 10 - kauft man ihn, wird statt eines
//   Inventareintrags ein GameFlags-Flag `learned_<id>` gesetzt.)
// ScalingStat (nur bei Waffen relevant): "strength" (Nahkampf) | "dexterity" (Bögen)
// DamageType (nur bei Waffen relevant, siehe doc/konzept/Gameplay/Kampfsystem.md):
//   "slashing" (Schneid) | "blunt" (Stoß) | "mixed" (Axt-Mischschaden) | "ranged" (Bogen)
// WeaponCategory (nur bei Waffen relevant): "onehand" | "twohand" | "bow" - steuert, welches
// Trainings-Flag noetig ist, damit die Combo aus Player.cs greift (siehe SpellCaster-Analogie).
public class ItemDefinition
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Type { get; set; } = "material";
	public string? ScalingStat { get; set; }
	public int Damage { get; set; } = 0;
	public string? DamageType { get; set; }
	public string? WeaponCategory { get; set; }
	public bool Stackable { get; set; } = true;

	// Rüstung/Schild (siehe doc/konzept/Items/Ruestung-Schilde.md)
	public int Defense { get; set; } = 0;
	public int StaggerResist { get; set; } = 0;

	// Verbrauchsgüter (siehe doc/konzept/Items/Verbrauchsgueter-Questitems.md)
	public int HealAmount { get; set; } = 0;

	// Handel (siehe doc/TODO.md Milestone 9): Kaufpreis beim Händler, Verkaufspreis ist die
	// Haelfte (siehe Hud.cs). 0 = nicht handelbar (z.B. Questgegenstaende).
	public int Price { get; set; } = 0;
}
