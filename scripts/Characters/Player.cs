using Godot;
using RPG.Combat;
using RPG.Data;
using RPG.Dialogue;
using RPG.Items;
using RPG.Magic;
using RPG.Quests;
using RPG.World;

namespace RPG.Characters;

public partial class Player : CharacterBody3D, IDamageable
{
	[Export] public float Speed = 5.0f;
	[Export] public float Acceleration = 20.0f;
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float MouseSensitivity = 0.003f;
	[Export] public float MinPitchDegrees = -80f;
	[Export] public float MaxPitchDegrees = 80f;
	[Export] public float JumpVelocity = 4.5f;

	// Kampf-Grundwerte (siehe doc/konzept/Gameplay/Kampfsystem.md). Bewusste Vereinfachung v1:
	// kein Windup/Aktiv-Fenster (kein Animationssystem), Blocktreffer statt echtem Timing-Parry -
	// siehe doc/TODO.md. Combo aktuell nur simples Links/Rechts-Abwechseln mit Cooldown-/Schadens-
	// bonus; echtes Timing-Fenster + waffentraining-abhaengige Freischaltung ist spaetere Arbeit.
	private const int BareHandDamage = 3;
	private const string BareHandDamageType = "blunt";
	private const float FluidAttackCooldown = 0.45f; // Cooldown nach einem alternierenden ("fluessigen") Treffer
	private const float ChoppyAttackCooldown = 0.7f; // Cooldown, wenn zweimal dieselbe Seite geklickt wird
	private const float ComboWindowSeconds = 1.2f; // Zeitfenster, in dem ein Wechsel noch als "fluessig" zaehlt
	private const int ComboDamageBonus = 2; // Schadensbonus pro Combo-Stufe
	private const int MaxComboStacks = 5;
	private const int StaggerDamageThreshold = 10;
	private const float HitStaggerDuration = 1.0f;
	private const float BareBlockStaggerDuration = 0.6f;

	private enum AttackSide { None, Left, Right }

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private Node3D _cameraPivot = null!;
	private CharacterStats _stats = null!;
	private Equipment _equipment = null!;
	private Area3D _meleeHitbox = null!;
	private SpellCaster _spellCaster = null!;

	private bool _isBlocking;
	private float _attackCooldownTimer;
	private float _staggerTimer;
	private AttackSide _lastAttackSide = AttackSide.None;
	private float _comboWindowTimer;
	private int _comboCount;

	// Kamera-Ausrichtung wird unabhängig von der Spieler-Rotation geführt,
	// damit die Kamera frei um den Charakter kreisen kann (wie in modernen 3rd-Person-RPGs).
	private float _cameraYaw;
	private float _cameraPitch;

	public override void _Ready()
	{
		_cameraPivot = GetNode<Node3D>("CameraPivot");
		_stats = GetNode<CharacterStats>("Stats");
		_equipment = GetNode<Equipment>("Equipment");
		_meleeHitbox = GetNode<Area3D>("MeleeHitbox");
		_spellCaster = GetNode<SpellCaster>("SpellCaster");
		Input.MouseMode = Input.MouseModeEnum.Captured; // Maus fürs Kamera-Look fangen

		_cameraYaw = Rotation.Y;

		AddToGroup("player");

		DialogueRunner.Instance.DialogueEnded += () => Input.MouseMode = Input.MouseModeEnum.Captured;

		GetNode<Inventory>("Inventory").ItemAdded += (itemId, totalCount) =>
			QuestManager.Instance.NotifyItemCollected(itemId, totalCount);

		// Ausgangssituation laut Konzept (doc/konzept/Story/Haupthandlung.md): der Spieler startet
		// bereits als Bote mit einem Auftrag, nicht erst durch ein NPC-Gespraech. StartQuest ist
		// selbst idempotent (siehe QuestManager), ein erneuter Aufruf nach dem Laden eines
		// Spielstands ist daher unschaedlich - RestoreActiveQuests ueberschreibt den Zustand ohnehin.
		if (!GameFlags.Instance.HasFlag("quest_started_quest_der_bote"))
			QuestManager.Instance.StartQuest("quest_der_bote");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (DialogueRunner.Instance.IsActive)
			return;

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_cameraYaw -= mouseMotion.Relative.X * MouseSensitivity;

			_cameraPitch -= mouseMotion.Relative.Y * MouseSensitivity;
			_cameraPitch = Mathf.Clamp(_cameraPitch, Mathf.DegToRad(MinPitchDegrees), Mathf.DegToRad(MaxPitchDegrees));
		}
	}

	// Fuers Laden eines Savegames: Position/Drehung setzen UND den intern getrackten Kamera-Yaw
	// nachziehen, sonst "springt" die Kamera im naechsten Frame (Desync mit Rotation.Y).
	public void ApplySaveState(Vector3 position, float rotationY)
	{
		GlobalPosition = position;
		Rotation = new Vector3(0f, rotationY, 0f);
		_cameraYaw = rotationY;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (DialogueRunner.Instance.IsActive)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			Velocity = Vector3.Zero;
			return;
		}

		// Goettliche Magie braucht eine Kanalzeit mit Bewegungssperre (siehe
		// doc/konzept/Gameplay/Magiesystem.md) - gleiches Einfrieren wie waehrend eines Dialogs.
		if (_spellCaster.IsChanneling)
		{
			Velocity = Vector3.Zero;
			return;
		}

		Vector3 velocity = Velocity;

		// Schwerkraft anwenden
		if (!IsOnFloor())
			velocity.Y -= _gravity * dt;

		// Sprung
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

		if (_attackCooldownTimer > 0f)
			_attackCooldownTimer -= dt;

		if (_comboWindowTimer > 0f)
			_comboWindowTimer -= dt;

		if (_staggerTimer > 0f)
		{
			_staggerTimer -= dt;
			_isBlocking = false;
		}
		else
		{
			_isBlocking = Input.IsActionPressed("block");
			if (!_isBlocking && _attackCooldownTimer <= 0f)
			{
				if (Input.IsActionJustPressed("attack_left"))
					PerformAttack(AttackSide.Left);
				else if (Input.IsActionJustPressed("attack_right"))
					PerformAttack(AttackSide.Right);
			}

			if (Input.IsActionJustPressed("cast_spell_1"))
				_spellCaster.TryCast(0);
			else if (Input.IsActionJustPressed("cast_spell_2"))
				_spellCaster.TryCast(1);
			else if (Input.IsActionJustPressed("cast_spell_3"))
				_spellCaster.TryCast(2);
		}

		// Bewegungsrichtung relativ zur Kamera auslesen (nicht zur Spieler-Rotation)
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Basis cameraBasis = new Basis(Vector3.Up, _cameraYaw);
		Vector3 direction = (cameraBasis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		Vector3 targetVelocity = direction * Speed;
		velocity.X = Mathf.MoveToward(velocity.X, targetVelocity.X, Acceleration * dt);
		velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocity.Z, Acceleration * dt);

		// Charakter sanft in Bewegungsrichtung drehen, statt hart mit der Maus
		if (direction != Vector3.Zero)
		{
			float targetAngle = Mathf.Atan2(-direction.X, -direction.Z);
			Rotation = new Vector3(0f, Mathf.LerpAngle(Rotation.Y, targetAngle, RotationSpeed * dt), 0f);
		}

		Velocity = velocity;
		MoveAndSlide();

		// Kamera-Rotation erst NACH der Körperdrehung setzen, sonst hinkt sie
		// bei schnellen Richtungswechseln einen Frame hinterher und "zuckt".
		_cameraPivot.Rotation = new Vector3(_cameraPitch, _cameraYaw - Rotation.Y, 0f);
	}

	// Kein Animations-Timing vorhanden (siehe doc/TODO.md) - der Treffer wird beim Tastendruck
	// sofort anhand der aktuellen Ueberlappung von MeleeHitbox ausgewertet statt eines
	// Windup/Aktiv-Fensters. Linksklick/Rechtsklick muessen abgewechselt werden, um den vollen
	// Combo-Bonus + kuerzeren Folge-Cooldown zu bekommen ("fluessig schlagen") - zweimal dieselbe
	// Seite (oder zu langsam) setzt die Combo zurueck und ist spuerbar traeger.
	// Die Combo selbst muss aber erst bei einem Lehrer erlernt werden (siehe doc/TODO.md
	// Milestone 10) - ohne das passende Flag gibt's nur Basisangriffe, egal wie man klickt.
	private void PerformAttack(AttackSide side)
	{
		ItemDefinition? weapon = _equipment.EquippedWeaponId != null ? GameData.Instance.GetItem(_equipment.EquippedWeaponId) : null;
		bool comboUnlocked = IsComboUnlocked(weapon?.WeaponCategory);

		bool isFluid = comboUnlocked && side != _lastAttackSide && _comboWindowTimer > 0f;
		_comboCount = isFluid ? Mathf.Min(_comboCount + 1, MaxComboStacks) : 1;
		_lastAttackSide = side;
		_comboWindowTimer = comboUnlocked ? ComboWindowSeconds : 0f;
		_attackCooldownTimer = isFluid ? FluidAttackCooldown : ChoppyAttackCooldown;

		int baseDamage = (weapon?.Damage ?? BareHandDamage) + _stats.Definition.Strength / 2;
		int damage = baseDamage + (_comboCount - 1) * ComboDamageBonus;
		string damageType = weapon?.DamageType ?? BareHandDamageType;

		foreach (Node3D body in _meleeHitbox.GetOverlappingBodies())
		{
			if (body != this && body is IDamageable damageable)
				damageable.TakeDamage(damage, damageType);
		}
	}

	// Waffenkategorie -> welches Trainings-Flag noetig ist (siehe ItemDefinition.WeaponCategory,
	// Data/Items/skill_*.json). Bloße Hand/Bogen haben aktuell keine Combo-Mechanik.
	private static bool IsComboUnlocked(string? weaponCategory) => weaponCategory switch
	{
		"onehand" => GameFlags.Instance.HasFlag("learned_onehanded_combo"),
		"twohand" => GameFlags.Instance.HasFlag("learned_twohanded_combo"),
		_ => false,
	};

	// IDamageable: Gegner-Treffer gegen den Spieler. DamageType wird hier ignoriert (Spieler hat
	// keine Resistenzen/Schwaechen, siehe CharacterDefinition-Kommentar) - stattdessen zaehlen
	// Ruestung (flacher Abzug) und ob gerade geblockt wird.
	public void TakeDamage(int amount, string? damageType)
	{
		ItemDefinition? armor = _equipment.EquippedArmorId != null ? GameData.Instance.GetItem(_equipment.EquippedArmorId) : null;
		int afterArmor = Mathf.Max(1, amount - (armor?.Defense ?? 0));
		int finalAmount = afterArmor;

		if (_isBlocking)
		{
			bool hasShield = _equipment.EquippedShieldId != null;
			finalAmount = Mathf.RoundToInt(afterArmor * (hasShield ? 0.2f : 0.6f));
			if (!hasShield)
				_staggerTimer = BareBlockStaggerDuration;
		}
		else if (afterArmor >= StaggerDamageThreshold)
		{
			_staggerTimer = HitStaggerDuration;
		}

		_stats.TakeDamage(finalAmount);
	}
}
