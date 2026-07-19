using System.Collections.Generic;

namespace RPG.Data;

public class CharacterDefinition
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public int MaxHealth { get; set; } = 10;
	public int Strength { get; set; } = 0;
	public int Dexterity { get; set; } = 0;
	public string? DialogueId { get; set; }

	// Gegner-Felder (siehe doc/konzept/Gameplay/Kampfsystem.md, scripts/Combat/Enemy.cs) - bei
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

	// Händler-Sortiment (siehe doc/TODO.md Milestone 9) - leer = kein Händler, Dialog kann trotzdem
	// eine "Handeln"-Choice haben, das Shop-Panel zeigt dann einfach nichts zum Kaufen an.
	public List<string> ShopItemIds { get; set; } = new();
}
