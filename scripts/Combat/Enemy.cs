using Godot;
using RPG.Characters;
using RPG.Data;
using RPG.Items;
using RPG.Quests;
using System.Collections.Generic;

namespace RPG.Combat;

// Einfacher Nahkampfgegner (siehe doc/konzept/Gameplay/Kampfsystem.md): verfolgt den Spieler
// innerhalb AggroRadius, greift in AttackRange direkt an (kein eigenes Hitbox-Timing wie beim
// Spieler - vereinfachte Gegner-KI fuer v1, siehe doc/TODO.md Milestone 2).
// Nutzt dieselbe Datenquelle wie Npc (CharacterDefinition/CharacterStats aus Data/Characters/),
// nur mit den Gegner-Feldern befuellt statt DialogueId.
public partial class Enemy : CharacterBody3D, IDamageable
{
	[Export] public string CharacterId = "";

	// Schaden ab dieser Hoehe staggert (kurze Angriffssperre) - siehe Kampfsystem.md.
	private const int StaggerDamageThreshold = 8;
	private const float StaggerDuration = 1.2f;

	private readonly float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private CharacterDefinition _definition = null!;
	private CharacterStats _stats = null!;
	private Node3D? _target;
	private float _attackTimer;
	private float _staggerTimer;

	public override void _Ready()
	{
		_definition = GameData.Instance.GetCharacter(CharacterId) ?? new CharacterDefinition { Id = CharacterId, Name = CharacterId };
		_stats = GetNode<CharacterStats>("Stats");
		_stats.Died += OnDied;
		AddToGroup("enemies");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (_stats.IsDead)
			return;

		Vector3 velocity = Velocity;
		if (!IsOnFloor())
			velocity.Y -= _gravity * dt;

		if (_staggerTimer > 0f)
		{
			_staggerTimer -= dt;
			velocity.X = 0f;
			velocity.Z = 0f;
			Velocity = velocity;
			MoveAndSlide();
			return;
		}

		_target ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
		velocity.X = 0f;
		velocity.Z = 0f;

		if (_target != null)
		{
			float distance = GlobalPosition.DistanceTo(_target.GlobalPosition);
			if (distance <= _definition.AttackRange)
			{
				FaceTarget(_target.GlobalPosition);
				TryAttack(dt);
			}
			else if (distance <= _definition.AggroRadius)
			{
				Vector3 direction = (_target.GlobalPosition - GlobalPosition) with { Y = 0f };
				direction = direction.Normalized();
				velocity.X = direction.X * _definition.MoveSpeed;
				velocity.Z = direction.Z * _definition.MoveSpeed;
				FaceTarget(_target.GlobalPosition);
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public void TakeDamage(int amount, string? damageType)
	{
		if (_stats.IsDead || amount <= 0)
			return;

		float multiplier = 1.0f;
		if (damageType != null && damageType == _definition.WeakTo)
			multiplier = 1.4f;
		else if (damageType != null && damageType == _definition.ResistantTo)
			multiplier = 0.6f;

		int finalAmount = Mathf.Max(1, Mathf.RoundToInt(amount * multiplier));
		_stats.TakeDamage(finalAmount);

		if (finalAmount >= StaggerDamageThreshold)
			_staggerTimer = StaggerDuration;
	}

	private void TryAttack(float dt)
	{
		_attackTimer -= dt;
		if (_attackTimer > 0f)
			return;

		_attackTimer = _definition.AttackCooldown;
		if (_target is IDamageable damageable)
			damageable.TakeDamage(_definition.AttackDamage, _definition.AttackDamageType);
	}

	private void FaceTarget(Vector3 targetPosition)
	{
		Vector3 direction = (targetPosition - GlobalPosition) with { Y = 0f };
		if (direction.LengthSquared() < 0.0001f)
			return;

		LookAt(GlobalPosition - direction, Vector3.Up);
	}

	// Beim Tod fallen die Loot-Items als ItemPickups an der Sterbeposition zu Boden, statt direkt
	// im Inventar zu landen - passt zum bestehenden ItemPickup-Muster (siehe Objects/Items/).
	private void OnDied()
	{
		QuestManager.Instance.NotifyEnemyKilled(CharacterId);

		PackedScene pickupScene = GD.Load<PackedScene>("res://Objects/Items/ItemPickup.tscn");
		Node parent = GetParent();

		foreach (string itemId in _definition.LootItemIds)
		{
			ItemPickup pickup = pickupScene.Instantiate<ItemPickup>();
			pickup.ItemId = itemId;
			parent.AddChild(pickup);
			pickup.GlobalPosition = GlobalPosition;
		}

		QueueFree();
	}
}
