using Godot;
using RPG.Characters;
using RPG.Combat;
using RPG.Data;
using System.Collections.Generic;

namespace RPG.Magic;

// Kanalisiert Zauber ueber CharacterStats.CurrentHealth als gemeinsame Ressource (siehe
// doc/architektur.md "Stats & Magie"). Sitzt als Sibling von Stats/Inventory/Equipment (Player).
//
// Bewusste Vereinfachungen v1 (siehe doc/TODO.md Milestone 3): feste 3 Zauber-Slots statt
// Zauberbuch/Lern-UI, kein echtes Projektil (arkane Zauber treffen sofort den naechsten Gegner
// in Reichweite), Blutmagie erlaubt nur einen aktiven Fluch gleichzeitig, goettliche Heilung wird
// nach der Kanalzeit auf einmal angewendet statt tatsaechlich ueber Zeit tropfenweise.
public partial class SpellCaster : Node
{
	[Export] public string[] KnownSpellIds = { "spell_feuerball", "spell_fluchsiegel", "spell_segnung" };

	[Signal] public delegate void ChannelingStartedEventHandler(string spellId);
	[Signal] public delegate void ChannelingEndedEventHandler();

	public bool IsChanneling { get; private set; }

	private const float CurseDuration = 4f;
	private const float CurseTickInterval = 1f;

	private CharacterStats _stats = null!;
	private Node3D _origin = null!;
	private readonly float[] _cooldownTimers = new float[3];

	private float _channelTimer;
	private SpellDefinition? _channelingSpell;

	private SpellDefinition? _activeCurse;
	private float _curseTimer;
	private float _curseTickTimer;

	public override void _Ready()
	{
		_origin = GetParent<Node3D>();
		_stats = _origin.GetNode<CharacterStats>("Stats");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		for (int i = 0; i < _cooldownTimers.Length; i++)
			if (_cooldownTimers[i] > 0f)
				_cooldownTimers[i] -= dt;

		if (IsChanneling)
			TickChannel(dt);

		if (_activeCurse != null)
			TickCurse(dt);
	}

	public void TryCast(int slot)
	{
		if (slot < 0 || slot >= KnownSpellIds.Length || IsChanneling)
			return;

		string spellId = KnownSpellIds[slot];
		if (string.IsNullOrEmpty(spellId) || _cooldownTimers[slot] > 0f)
			return;

		SpellDefinition? spell = GameData.Instance.GetSpell(spellId);
		if (spell == null || _stats.CurrentHealth <= spell.HealthCost)
			return; // Zauber darf den Wirkenden nicht toeten - Leben ist die Ressource, kein Selbstmordknopf

		_cooldownTimers[slot] = spell.Cooldown;
		_stats.TakeDamage(spell.HealthCost);

		switch (spell.School)
		{
			case "divine":
				StartChannel(spell);
				break;
			case "blood":
				CastBlood(spell);
				break;
			default: // "arcane"
				CastArcane(spell);
				break;
		}
	}

	private void CastArcane(SpellDefinition spell)
	{
		Enemy? target = FindNearestEnemy(spell.Range);
		target?.TakeDamage(spell.Damage, spell.School);
	}

	private void CastBlood(SpellDefinition spell)
	{
		_activeCurse = spell;
		_curseTimer = CurseDuration;
		_curseTickTimer = CurseTickInterval;
	}

	private void TickCurse(float dt)
	{
		_curseTimer -= dt;
		_curseTickTimer -= dt;

		if (_curseTickTimer <= 0f && _activeCurse != null)
		{
			_curseTickTimer = CurseTickInterval;
			int gained = 0;
			foreach (Enemy enemy in FindEnemiesInRange(_activeCurse.Range))
			{
				enemy.TakeDamage(_activeCurse.Damage, _activeCurse.School);
				gained += _activeCurse.Damage;
			}

			if (gained > 0)
				_stats.Heal(gained);
		}

		if (_curseTimer <= 0f)
			_activeCurse = null;
	}

	private void StartChannel(SpellDefinition spell)
	{
		IsChanneling = true;
		_channelingSpell = spell;
		_channelTimer = spell.CastTime;
		EmitSignal(SignalName.ChannelingStarted, spell.Id);
	}

	private void TickChannel(float dt)
	{
		_channelTimer -= dt;
		if (_channelTimer > 0f)
			return;

		if (_channelingSpell != null)
			_stats.Heal(_channelingSpell.HealAmount);

		IsChanneling = false;
		_channelingSpell = null;
		EmitSignal(SignalName.ChannelingEnded);
	}

	private Enemy? FindNearestEnemy(float range)
	{
		Enemy? nearest = null;
		float nearestDistance = range;

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Enemy enemy || !IsInstanceValid(enemy))
				continue;

			float distance = _origin.GlobalPosition.DistanceTo(enemy.GlobalPosition);
			if (distance <= nearestDistance)
			{
				nearest = enemy;
				nearestDistance = distance;
			}
		}

		return nearest;
	}

	private List<Enemy> FindEnemiesInRange(float range)
	{
		List<Enemy> result = new();

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is Enemy enemy && IsInstanceValid(enemy) && _origin.GlobalPosition.DistanceTo(enemy.GlobalPosition) <= range)
				result.Add(enemy);
		}

		return result;
	}
}
