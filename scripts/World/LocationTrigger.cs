using Godot;
using RPG.Quests;

namespace RPG.World;

// Area3D, die beim Betreten durch den Spieler einmalig ein Flag setzt (z.B. "Ort X betreten").
// Groesse/Form der CollisionShape3D wird pro Platzierung im Editor angepasst.
public partial class LocationTrigger : Area3D
{
	[Export] public string FlagId = "";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (!body.IsInGroup("player"))
			return;

		GameFlags.Instance.SetFlag(FlagId);
		QuestManager.Instance.NotifyLocationEntered(FlagId);
	}
}
