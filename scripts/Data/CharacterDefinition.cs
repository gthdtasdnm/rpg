using System.Collections.Generic;

namespace RPG.Data;

public class CharacterDefinition
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public int MaxHealth { get; set; } = 10;
	public int Strength { get; set; } = 0;
	public int Dexterity { get; set; } = 0;

	// Kein DialogueId mehr: welcher Dialog zu einem NPC gehoert, steht am Npc-Node im Inspector
	// (Npc.cs: DialogueFile/DialogueTitle). Ein Charakter kann so in verschiedenen Szenen
	// unterschiedliche Dialoge fuehren, ohne dass es dafuer mehrere JSON-Eintraege braucht.

	// Gegner-Felder (siehe doc/spielsysteme.md, scripts/Combat/Enemy.cs) - bei
	// Spieler/NPCs ungenutzt (Defaults). Bleiben in CharacterDefinition statt einer eigenen
	// EnemyDefinition, weil Gegner dieselbe Health/Strength/Dexterity-Basis wie jeder Charakter
	// brauchen und sonst zwei parallele Datenquellen fuer denselben Zweck entstuenden.
	public int AttackDamage { get; set; } = 0;
	public string? AttackDamageType { get; set; } // slashing|blunt|mixed
	public string? ResistantTo { get; set; } // DamageType, gegen den dieser Gegner reduzierten Schaden nimmt
	public string? WeakTo { get; set; } // DamageType, gegen den dieser Gegner erhöhten Schaden nimmt
	public float AggroRadius { get; set; } = 0f; // 0 = keine KI (Standardwert fuer Spieler/NPCs)
	public float AttackRange { get; set; } = 1.5f;
	public float AttackCooldown { get; set; } = 1.5f;
	public float MoveSpeed { get; set; } = 3.0f;
	public List<string> LootItemIds { get; set; } = new();

	// Händler-Sortiment (siehe doc/spielsysteme.md, Abschnitt 5) - leer = kein Händler, Dialog kann trotzdem
	// eine "Handeln"-Choice haben, das Shop-Panel zeigt dann einfach nichts zum Kaufen an.
	public List<string> ShopItemIds { get; set; } = new();
}
