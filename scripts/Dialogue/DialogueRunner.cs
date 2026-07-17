using Godot;
using RPG.Data;
using RPG.World;
using System.Collections.Generic;

namespace RPG.Dialogue;

// Autoload: fuehrt einen Dialogbaum (aus Data/Dialogues/*.json) Schritt fuer Schritt aus.
// Kennt Quests nicht direkt - meldet "startQuest" nur per Signal weiter (siehe QuestManager).
// Knoten/Choices koennen ueber Flags (GameFlags) bedingt sein, z.B. um NPCs anders reagieren
// zu lassen, wenn eine Quest schon laeuft oder erfuellt ist.
public partial class DialogueRunner : Node
{
	public static DialogueRunner Instance { get; private set; } = null!;

	[Signal] public delegate void LineChangedEventHandler(string speaker, string text, string[] choices);
	[Signal] public delegate void DialogueEndedEventHandler();
	[Signal] public delegate void QuestStartRequestedEventHandler(string questId);
	[Signal] public delegate void QuestCompleteRequestedEventHandler(string questId);

	public bool IsActive { get; private set; }

	private DialogueDefinition? _dialogue;
	private string _speaker = "";
	private readonly List<DialogueChoice> _visibleChoices = new();

	public override void _Ready()
	{
		Instance = this;
	}

	public void Start(string dialogueId, string speaker = "")
	{
		DialogueDefinition? dialogue = GameData.Instance.GetDialogue(dialogueId);
		if (dialogue == null)
		{
			GD.PushError($"DialogueRunner: unbekannte DialogueId '{dialogueId}'");
			return;
		}

		_dialogue = dialogue;
		_speaker = speaker;
		IsActive = true;

		string startNode = dialogue.StartNode;
		foreach (DialogueStartRule rule in dialogue.StartRules)
		{
			if (IsConditionMet(rule.RequiresFlag, rule.RequiresNotFlag))
			{
				startNode = rule.Node;
				break;
			}
		}

		GoToNode(startNode);
	}

	public void Choose(int index)
	{
		if (index < 0 || index >= _visibleChoices.Count)
			return;

		DialogueChoice choice = _visibleChoices[index];

		if (choice.SetFlag != null)
			GameFlags.Instance.SetFlag(choice.SetFlag);

		if (choice.StartQuest != null)
			EmitSignal(SignalName.QuestStartRequested, choice.StartQuest);

		if (choice.CompleteQuest != null)
			EmitSignal(SignalName.QuestCompleteRequested, choice.CompleteQuest);

		if (choice.Next == null)
		{
			End();
			return;
		}

		GoToNode(choice.Next);
	}

	private void GoToNode(string nodeId)
	{
		if (_dialogue == null || !_dialogue.Nodes.TryGetValue(nodeId, out DialogueNode? node))
		{
			End();
			return;
		}

		_visibleChoices.Clear();
		foreach (DialogueChoice choice in node.Choices)
		{
			if (IsConditionMet(choice.RequiresFlag, choice.RequiresNotFlag))
				_visibleChoices.Add(choice);
		}

		string[] choiceTexts = new string[_visibleChoices.Count];
		for (int i = 0; i < _visibleChoices.Count; i++)
			choiceTexts[i] = _visibleChoices[i].Text;

		EmitSignal(SignalName.LineChanged, _speaker, node.Text, choiceTexts);
	}

	private static bool IsConditionMet(string? requiresFlag, string? requiresNotFlag)
	{
		if (requiresFlag != null && !GameFlags.Instance.HasFlag(requiresFlag))
			return false;

		if (requiresNotFlag != null && GameFlags.Instance.HasFlag(requiresNotFlag))
			return false;

		return true;
	}

	private void End()
	{
		IsActive = false;
		_dialogue = null;
		_visibleChoices.Clear();
		EmitSignal(SignalName.DialogueEnded);
	}
}
