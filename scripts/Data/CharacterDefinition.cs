namespace RPG.Data;

public class CharacterDefinition
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public int MaxHealth { get; set; } = 10;
	public int Strength { get; set; } = 0;
	public int Dexterity { get; set; } = 0;
	public string? DialogueId { get; set; }
}
