using Godot;
using RPG.Data;

namespace RPG.Characters;

// Health/Strength/Dexterity eines Charakters, geladen aus Data/Characters/<CharacterId>.json.
// CurrentHealth dient später auch als gemeinsame Ressource fuer goettliche/daemonische Magie.
public partial class CharacterStats : Node
{
	[Export] public string CharacterId = "";

	[Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
	[Signal] public delegate void DiedEventHandler();

	public CharacterDefinition Definition { get; private set; } = null!;
	public int CurrentHealth { get; private set; }
	public bool IsDead => CurrentHealth <= 0;

	public override void _Ready()
	{
		CharacterDefinition? definition = GameData.Instance.GetCharacter(CharacterId);
		if (definition == null)
		{
			GD.PushError($"CharacterStats: unbekannte CharacterId '{CharacterId}'");
			definition = new CharacterDefinition { Id = CharacterId, Name = CharacterId };
		}

		Definition = definition;
		CurrentHealth = Definition.MaxHealth;
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || IsDead)
			return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, Definition.MaxHealth);

		if (IsDead)
			EmitSignal(SignalName.Died);
	}

	public void Heal(int amount)
	{
		if (amount <= 0 || IsDead)
			return;

		CurrentHealth = Mathf.Min(Definition.MaxHealth, CurrentHealth + amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, Definition.MaxHealth);
	}

	// Fuer Savegame-Laden: setzt den Wert direkt, ohne Tod/Heal-Regeln zu pruefen.
	public void RestoreHealth(int value)
	{
		CurrentHealth = Mathf.Clamp(value, 0, Definition.MaxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, Definition.MaxHealth);
	}
}
