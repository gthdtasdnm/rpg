using Godot;
using RPG.Dialogue;

namespace RPG.World;

// Area3D + physische Barriere fuer Zugangskontrollen (siehe doc/TODO.md Milestone 11): ohne
// RequiredFlagId bleibt eine CollisionShape3D aktiv und blockiert den Weg physisch. Betritt der
// Spieler die Checkpoint-Area3D trotzdem (z.B. laeuft dagegen), startet ein erzwungener Dialog
// mit der Wache, die erklaert, warum kein Zutritt ist. Sobald das Flag gesetzt wird (z.B. durch
// eine andere Quest/ein Gespraech), gibt die Barriere automatisch den Weg frei.
public partial class GuardCheckpoint : Area3D
{
	[Export] public string RequiredFlagId = "";
	[Export] public string GuardDialogueId = "";
	[Export] public string GuardName = "";
	[Export] public string GuardCharacterId = "";

	private CollisionShape3D _barrierShape = null!;
	private MeshInstance3D? _barrierMesh;

	public override void _Ready()
	{
		_barrierShape = GetNode<CollisionShape3D>("Barrier/CollisionShape3D");
		_barrierMesh = GetNodeOrNull<MeshInstance3D>("Barrier/MeshInstance3D");
		UpdateBarrier();

		GameFlags.Instance.FlagChanged += OnFlagChanged;
		BodyEntered += OnBodyEntered;
	}

	private void OnFlagChanged(string flagId, bool value)
	{
		if (flagId == RequiredFlagId)
			UpdateBarrier();
	}

	private void UpdateBarrier()
	{
		bool open = GameFlags.Instance.HasFlag(RequiredFlagId);
		_barrierShape.Disabled = open;
		if (_barrierMesh != null)
			_barrierMesh.Visible = !open;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (!body.IsInGroup("player") || GameFlags.Instance.HasFlag(RequiredFlagId))
			return;

		DialogueRunner.Instance.Start(GuardDialogueId, GuardName, GuardCharacterId);
	}
}
