using Godot;
using RPG.Data;
using RPG.Dialogue;
using RPG.Items;
using RPG.World;
using System.Collections.Generic;

namespace RPG.Quests;

// Autoload: haelt Fortschritt aktiver Quests. Kennt nur "collect_item" als Objective-Typ,
// weitere Typen koennen additiv ergaenzt werden (neuer Fall in NotifyXyz + Schema-Erweiterung).
public partial class QuestManager : Node
{
	public static QuestManager Instance { get; private set; } = null!;

	[Signal] public delegate void QuestStartedEventHandler(string questId, string title);
	[Signal] public delegate void QuestProgressedEventHandler(string questId, string title, string progressText);
	[Signal] public delegate void QuestCompletedEventHandler(string questId, string title);

	private class QuestState
	{
		public required QuestDefinition Definition;
		public required int[] Progress;
	}

	public record QuestLogEntry(string Title, string Status, bool IsActive);

	private readonly Dictionary<string, QuestState> _active = new();

	public override void _Ready()
	{
		Instance = this;
		DialogueRunner.Instance.QuestStartRequested += StartQuest;
		DialogueRunner.Instance.QuestCompleteRequested += CompleteQuest;
	}

	public void StartQuest(string questId)
	{
		if (_active.ContainsKey(questId) || GameFlags.Instance.HasFlag($"quest_completed_{questId}"))
			return;

		QuestDefinition? definition = GameData.Instance.GetQuest(questId);
		if (definition == null)
		{
			GD.PushError($"QuestManager: unbekannte QuestId '{questId}'");
			return;
		}

		QuestState state = new() { Definition = definition, Progress = new int[definition.Objectives.Count] };
		_active[questId] = state;

		// Bereits vor Quest-Start eingesammelte Items sofort werten (Reihenfolge soll egal sein).
		Inventory? inventory = GetPlayerInventory();
		if (inventory != null)
		{
			for (int i = 0; i < definition.Objectives.Count; i++)
			{
				QuestObjective objective = definition.Objectives[i];
				if (objective.Type == "collect_item")
					state.Progress[i] = Mathf.Min(inventory.GetCount(objective.ItemId), objective.Amount);
			}
		}

		GameFlags.Instance.SetFlag($"quest_started_{questId}");
		EmitSignal(SignalName.QuestStarted, questId, definition.Title);

		if (IsComplete(state))
			MarkReady(questId);
	}

	public void NotifyItemCollected(string itemId, int totalCount)
	{
		List<string> justReady = new();

		foreach (KeyValuePair<string, QuestState> pair in _active)
		{
			string questId = pair.Key;
			QuestState state = pair.Value;
			bool changed = false;

			for (int i = 0; i < state.Definition.Objectives.Count; i++)
			{
				QuestObjective objective = state.Definition.Objectives[i];
				if (objective.Type != "collect_item" || objective.ItemId != itemId)
					continue;

				int updated = Mathf.Min(totalCount, objective.Amount);
				if (updated != state.Progress[i])
				{
					state.Progress[i] = updated;
					changed = true;
				}
			}

			if (changed)
				EmitSignal(SignalName.QuestProgressed, questId, state.Definition.Title, BuildProgressText(state));

			if (IsComplete(state))
				justReady.Add(questId);
		}

		foreach (string questId in justReady)
			MarkReady(questId);
	}

	// Objectives erfuellt heisst noch nicht abgeschlossen - der Spieler muss die Quest erst bei
	// der gebenden Person abgeben (Dialog-Choice mit "completeQuest").
	private void MarkReady(string questId)
	{
		GameFlags.Instance.SetFlag($"quest_ready_{questId}");
	}

	// Wird ueber eine Dialog-Choice ausgeloest (DialogueChoice.CompleteQuest), nicht automatisch.
	private void CompleteQuest(string questId)
	{
		if (!_active.TryGetValue(questId, out QuestState? state) || !IsComplete(state))
			return;

		_active.Remove(questId);

		Inventory? inventory = GetPlayerInventory();
		if (inventory != null)
		{
			foreach (QuestObjective objective in state.Definition.Objectives)
			{
				if (objective.Type == "collect_item")
					inventory.RemoveItem(objective.ItemId, objective.Amount);
			}

			foreach (string rewardItemId in state.Definition.RewardItemIds)
				inventory.AddItem(rewardItemId);
		}

		GameFlags.Instance.SetFlag($"quest_completed_{questId}");
		EmitSignal(SignalName.QuestCompleted, questId, state.Definition.Title);
	}

	private Inventory? GetPlayerInventory()
	{
		Node? player = GetTree().GetFirstNodeInGroup("player");
		return player?.GetNodeOrNull<Inventory>("Inventory");
	}

	// Fuers Questlog-UI: alle Quests, mit denen der Spieler schon zu tun hatte (per Flag erkannt),
	// keine eigene Buchfuehrung noetig - Status kommt aus Flags + laufendem Fortschritt.
	public List<QuestLogEntry> GetQuestLogEntries()
	{
		List<QuestLogEntry> entries = new();

		foreach (QuestDefinition definition in GameData.Instance.GetAllQuests())
		{
			bool started = GameFlags.Instance.HasFlag($"quest_started_{definition.Id}");
			bool completed = GameFlags.Instance.HasFlag($"quest_completed_{definition.Id}");
			if (!started && !completed)
				continue;

			string status;
			if (completed)
				status = "Abgeschlossen";
			else if (GameFlags.Instance.HasFlag($"quest_ready_{definition.Id}"))
				status = "Bereit zur Abgabe";
			else
				status = _active.TryGetValue(definition.Id, out QuestState? state) ? BuildProgressText(state) : "";

			entries.Add(new QuestLogEntry(definition.Title, status, !completed));
		}

		return entries;
	}

	public Dictionary<string, int[]> GetActiveQuestProgressForSave()
	{
		Dictionary<string, int[]> result = new();
		foreach (KeyValuePair<string, QuestState> pair in _active)
			result[pair.Key] = pair.Value.Progress;

		return result;
	}

	public void RestoreActiveQuests(Dictionary<string, int[]> savedProgress)
	{
		_active.Clear();

		foreach (KeyValuePair<string, int[]> pair in savedProgress)
		{
			QuestDefinition? definition = GameData.Instance.GetQuest(pair.Key);
			if (definition == null)
				continue;

			_active[pair.Key] = new QuestState { Definition = definition, Progress = pair.Value };
		}
	}

	private static bool IsComplete(QuestState state)
	{
		for (int i = 0; i < state.Definition.Objectives.Count; i++)
			if (state.Progress[i] < state.Definition.Objectives[i].Amount)
				return false;

		return true;
	}

	private static string BuildProgressText(QuestState state)
	{
		List<string> parts = new();
		for (int i = 0; i < state.Definition.Objectives.Count; i++)
		{
			QuestObjective objective = state.Definition.Objectives[i];
			if (objective.Type != "collect_item")
				continue;

			string itemName = GameData.Instance.GetItem(objective.ItemId)?.Name ?? objective.ItemId;
			parts.Add($"{state.Progress[i]}/{objective.Amount} {itemName}");
		}

		return string.Join(", ", parts);
	}
}
