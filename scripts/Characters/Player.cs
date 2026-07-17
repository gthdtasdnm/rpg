using Godot;
using RPG.Dialogue;
using RPG.Items;
using RPG.Quests;

namespace RPG.Characters;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float Acceleration = 20.0f;
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float MouseSensitivity = 0.003f;
	[Export] public float MinPitchDegrees = -80f;
	[Export] public float MaxPitchDegrees = 80f;
	[Export] public float JumpVelocity = 4.5f;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private Node3D _cameraPivot = null!;

	// Kamera-Ausrichtung wird unabhängig von der Spieler-Rotation geführt,
	// damit die Kamera frei um den Charakter kreisen kann (wie in modernen 3rd-Person-RPGs).
	private float _cameraYaw;
	private float _cameraPitch;

	public override void _Ready()
	{
		_cameraPivot = GetNode<Node3D>("CameraPivot");
		Input.MouseMode = Input.MouseModeEnum.Captured; // Maus fürs Kamera-Look fangen

		_cameraYaw = Rotation.Y;

		AddToGroup("player");

		DialogueRunner.Instance.DialogueEnded += () => Input.MouseMode = Input.MouseModeEnum.Captured;

		GetNode<Inventory>("Inventory").ItemAdded += (itemId, totalCount) =>
			QuestManager.Instance.NotifyItemCollected(itemId, totalCount);
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

		Vector3 velocity = Velocity;

		// Schwerkraft anwenden
		if (!IsOnFloor())
			velocity.Y -= _gravity * dt;

		// Sprung
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

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
}
