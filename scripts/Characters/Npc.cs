using Godot;
using Godot.Collections;
using RPG.Data;
using RPG.Dialogue;
using RPG.Interaction;
using RPG.Quests;
using RPG.World;

namespace RPG.Characters;

// Ansprechbarer Charakter. Neuen NPC anlegen = World/npc.tscn instanzieren und im Inspector
// CharacterId + DialogueFile setzen. Kein Code noetig.
public partial class Npc : StaticBody3D, IInteractable
{
	// Verweist auf eine Datei in Data/Characters (ohne .json). Optional - ohne CharacterId
	// funktioniert der Dialog trotzdem, es gibt dann nur keine Werte/Flags fuer diesen NPC.
	[Export] public string CharacterId = "";

	// Nur noetig, wenn kein CharacterId gesetzt ist (oder der angezeigte Name abweichen soll).
	[Export] public string DisplayName = "";

	[Export(PropertyHint.File, "*.dialogue")] public string DialogueFile = "";

	// Titel, bei dem das Gespraech beginnt (die "~ titel"-Zeilen in der .dialogue-Datei).
	[Export] public string DialogueTitle = "start";

	// Setzt den NPC beim Start auf den Boden darunter. Spart im Editor das Hoehe-Suchen auf dem
	// Terrain - Position grob setzen reicht, Y wird beim Spielstart korrigiert.
	[Export] public bool SnapToGround = true;

	// --- Von selbst ansprechen (Gothic-Stil) ---------------------------------------------
	// Der NPC wartet nicht auf "E", sondern haelt den Spieler an, sobald er nah genug kommt.
	// Fuer Wachen, Wegelagerer, Werber - also ueberall dort, wo der Spieler nicht vorbeisoll,
	// ohne angesprochen worden zu sein.
	[Export] public bool GreetOnApproach = false;

	[Export(PropertyHint.Range, "1,25,0.5")] public float GreetRadius = 6f;

	// true = spricht nur beim allerersten Mal an (ueberlebt das Speichern, Flag "greeted_<id>").
	// false = jedes Mal, wenn der Spieler den Bereich neu betritt.
	[Export] public bool GreetOnlyOnce = true;

	// Dreht sich beim Ansprechen zum Spieler. Ohne das reden NPCs an einem vorbei.
	[Export] public bool TurnToPlayer = true;

	[Export(PropertyHint.Range, "1,20,0.5")] public float TurnSpeed = 6f;

	private CharacterDefinition? _definition;
	private Area3D? _greetArea;
	private bool _playerOutsideGreetArea = true;
	private float? _targetYaw;

	public override void _Ready()
	{
		if (!string.IsNullOrEmpty(CharacterId))
		{
			_definition = GameData.Instance.GetCharacter(CharacterId);
			if (_definition == null)
				GD.PushWarning($"Npc '{Name}': unbekannte CharacterId '{CharacterId}' (fehlt Data/Characters/{CharacterId}.json?)");
		}

		if (SnapToGround)
			DoSnapToGround();

		SetupGreetArea();
	}

	private void SetupGreetArea()
	{
		_greetArea = GetNodeOrNull<Area3D>("GreetArea");
		if (_greetArea == null)
			return;

		// Radius aus dem Inspector auf die Kugel uebertragen. Die Form wird kopiert, damit
		// nicht alle NPCs, die dieselbe Szene benutzen, denselben Radius teilen.
		if (_greetArea.GetNodeOrNull<CollisionShape3D>("CollisionShape3D") is CollisionShape3D shape
			&& shape.Shape is SphereShape3D sphere)
		{
			SphereShape3D own = (SphereShape3D)sphere.Duplicate();
			own.Radius = GreetRadius;
			shape.Shape = own;
		}

		_greetArea.Monitoring = GreetOnApproach;
		if (!GreetOnApproach)
			return;

		_greetArea.BodyEntered += OnGreetAreaEntered;
		_greetArea.BodyExited += OnGreetAreaExited;
	}

	private void OnGreetAreaEntered(Node3D body)
	{
		if (!body.IsInGroup("player") || !_playerOutsideGreetArea)
			return;

		_playerOutsideGreetArea = false;

		// Nicht dazwischenreden, wenn gerade ohnehin ein Gespraech laeuft.
		if (DialogueBridge.Instance.IsActive)
			return;

		if (GreetOnlyOnce && GameFlags.Instance.HasFlag(GreetedFlag))
			return;

		GameFlags.Instance.SetFlag(GreetedFlag);
		Interact(body);
	}

	private void OnGreetAreaExited(Node3D body)
	{
		if (body.IsInGroup("player"))
			_playerOutsideGreetArea = true;
	}

	private string GreetedFlag => $"greeted_{(string.IsNullOrEmpty(CharacterId) ? Name : CharacterId)}";

	public override void _Process(double delta)
	{
		if (_targetYaw is not float goal)
			return;

		float next = Mathf.LerpAngle(Rotation.Y, goal, (float)delta * TurnSpeed);
		if (Mathf.Abs(Mathf.AngleDifference(next, goal)) < 0.01f)
		{
			next = goal;
			_targetYaw = null;
		}

		Rotation = new Vector3(Rotation.X, next, Rotation.Z);
	}

	// Dreht den NPC (weich, ueber _Process) in Richtung eines Ziels. Nur um die Hochachse -
	// er soll sich nicht nach vorne kippen, wenn der Spieler tiefer oder hoeher steht.
	private void FaceTowards(Node3D target)
	{
		Vector3 direction = target.GlobalPosition - GlobalPosition;
		direction.Y = 0f;

		if (direction.LengthSquared() < 0.01f)
			return;

		direction = direction.Normalized();

		// Ein Node schaut entlang seiner -Z-Achse: bei Yaw θ ist das (-sin θ, 0, -cos θ).
		_targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
	}

	public string GetDisplayName()
	{
		if (!string.IsNullOrEmpty(DisplayName))
			return DisplayName;

		return _definition?.Name ?? Name;
	}

	public string GetPrompt() => $"E — Mit {GetDisplayName()} sprechen";

	public void Interact(Node interactor)
	{
		if (TurnToPlayer && interactor is Node3D speaker)
			FaceTowards(speaker);

		if (!string.IsNullOrEmpty(CharacterId))
		{
			GameFlags.Instance.SetFlag($"talked_{CharacterId}");
			QuestManager.Instance.NotifyNpcTalkedTo(CharacterId);
		}

		DialogueBridge.Instance.Show(DialogueFile, DialogueTitle, this);
	}

	private void DoSnapToGround()
	{
		Vector3 from = GlobalPosition + Vector3.Up * 10f;
		Vector3 to = GlobalPosition + Vector3.Down * 50f;

		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to);
		query.Exclude = new Array<Rid> { GetRid() };

		Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (hit.Count > 0)
			GlobalPosition = new Vector3(GlobalPosition.X, hit["position"].AsVector3().Y, GlobalPosition.Z);
	}
}
