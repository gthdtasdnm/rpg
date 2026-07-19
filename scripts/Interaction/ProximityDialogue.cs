using Godot;
using RPG.Dialogue;
using RPG.World;

namespace RPG.Interaction;

// Area3D fuer NPCs, die den Spieler von sich aus ansprechen (siehe doc/TODO.md Milestone 11),
// statt nur auf "E" zu warten (siehe Npc.cs). Loest einmalig (per GameFlags-Flag) einen Dialog
// aus, sobald der Spieler die Zone betritt - danach normal per Npc.Interact() weiter ansprechbar.
public partial class ProximityDialogue : Area3D
{
	[Export] public string DialogueId = "";
	[Export] public string SpeakerName = "";
	[Export] public string SpeakerCharacterId = "";
	[Export] public string TriggerFlagId = "";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (!body.IsInGroup("player") || GameFlags.Instance.HasFlag(TriggerFlagId))
			return;

		GameFlags.Instance.SetFlag(TriggerFlagId);
		DialogueRunner.Instance.Start(DialogueId, SpeakerName, SpeakerCharacterId);
	}
}
