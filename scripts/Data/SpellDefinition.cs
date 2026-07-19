namespace RPG.Data;

// School: "arcane" (alte Kampfmagie) | "blood" (Blutmagie) | "divine" (göttliche Magie/Gebete)
// siehe doc/konzept/Gameplay/Magiesystem.md und doc/architektur.md ("Stats & Magie").
// CastTime > 0 => Kanalzeit mit Bewegungssperre (nur bei "divine" sinnvoll genutzt).
public class SpellDefinition
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string School { get; set; } = "arcane";
	public int HealthCost { get; set; } = 0;
	public int Damage { get; set; } = 0;
	public int HealAmount { get; set; } = 0;
	public float CastTime { get; set; } = 0f;
	public float Range { get; set; } = 8f;
	public float Cooldown { get; set; } = 1f;
}
