using Godot;
using RPG.Data;
using RPG.Dialogue;
using RPG.Interaction;
using RPG.Quests;
using RPG.World;

namespace RPG.Characters;

public partial class Npc : StaticBody3D, IInteractable
{
	[Export] public string CharacterId = "";

	private CharacterDefinition _definition = null!;

	public override void _Ready()
	{
		CharacterDefinition? definition = GameData.Instance.GetCharacter(CharacterId);
		if (definition == null)
		{
			GD.PushError($"Npc: unbekannte CharacterId '{CharacterId}'");
			definition = new CharacterDefinition { Id = CharacterId, Name = CharacterId };
		}

		_definition = definition;
	}

	public string GetPrompt() => $"E - Mit {_definition.Name} sprechen";

	public void Interact(Node interactor)
	{
		GameFlags.Instance.SetFlag($"talked_{CharacterId}");
		QuestManager.Instance.NotifyNpcTalkedTo(CharacterId);

		if (string.IsNullOrEmpty(_definition.DialogueId))
			return;

		DialogueRunner.Instance.Start(_definition.DialogueId, _definition.Name, CharacterId);
	}
}
