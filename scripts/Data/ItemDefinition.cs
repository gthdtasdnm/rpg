namespace RPG.Data;

// "weapon" | "consumable" | "material" | "quest"
// ScalingStat (nur bei Waffen relevant): "strength" (Schwerter) | "dexterity" (Bögen)
public class ItemDefinition
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Type { get; set; } = "material";
	public string? ScalingStat { get; set; }
	public int Damage { get; set; } = 0;
	public bool Stackable { get; set; } = true;
}
