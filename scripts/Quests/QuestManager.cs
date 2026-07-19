using Godot;
using RPG.Data;
using RPG.Dialogue;
using RPG.Items;
using RPG.World;
using System.Collections.Generic;

namespace RPG.Quests;

// Autoload: haelt Fortschritt aktiver Quests. Objective-Typen siehe QuestDefinition.cs
// (collect_item/talk_to_npc/reach_location/kill_enemy) - weitere additiv ergaenzbar (neuer Fall
// in einer NotifyXyz-Methode + Schema-Erweiterung).
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

		// Bereits vor Quest-Start erfuellte Objectives sofort werten (Reihenfolge soll egal sein:
		// z.B. Item schon im Inventar, NPC schon angesprochen, Ort schon betreten).
		Inventory? inventory = GetPlayerInventory();
		for (int i = 0; i < definition.Objectives.Count; i++)
		{
			QuestObjective objective = definition.Objectives[i];
			switch (objective.Type)
			{
				case "collect_item" when inventory != null:
					state.Progress[i] = Mathf.Min(inventory.GetCount(objective.ItemId), objective.Amount);
					break;
				case "talk_to_npc" when GameFlags.Instance.HasFlag($"talked_{objective.TargetId}"):
					state.Progress[i] = objective.Amount;
					break;
				case "reach_location" when objective.TargetId != null && GameFlags.Instance.HasFlag(objective.TargetId):
					state.Progress[i] = objective.Amount;
					break;
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

	// Aehnlich NotifyItemCollected, nur ohne Inventar-Abgleich - Gegner-Kills werden nicht
	// rueckwirkend vor Quest-Start gewertet (kein historischer Kill-Zaehler ausserhalb der Quest).
	public void NotifyEnemyKilled(string characterId)
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
				if (objective.Type != "kill_enemy" || objective.TargetId != characterId || state.Progress[i] >= objective.Amount)
					continue;

				state.Progress[i]++;
				changed = true;
			}

			if (changed)
				EmitSignal(SignalName.QuestProgressed, questId, state.Definition.Title, BuildProgressText(state));

			if (IsComplete(state))
				justReady.Add(questId);
		}

		foreach (string questId in justReady)
			MarkReady(questId);
	}

	public void NotifyNpcTalkedTo(string npcId) => NotifyTargetReached("talk_to_npc", npcId);

	public void NotifyLocationEntered(string flagId) => NotifyTargetReached("reach_location", flagId);

	// Gemeinsame Logik fuer talk_to_npc/reach_location: beide sind reine "einmal erledigt"-Ziele
	// (TargetId trifft zu -> Objective sofort komplett), im Gegensatz zu collect_item mit Amount > 1.
	private void NotifyTargetReached(string objectiveType, string targetId)
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
				if (objective.Type != objectiveType || objective.TargetId != targetId || state.Progress[i] >= objective.Amount)
					continue;

				state.Progress[i] = objective.Amount;
				changed = true;
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

			if (state.Definition.RewardGold > 0)
				inventory.AddGold(state.Definition.RewardGold);
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
			bool done = state.Progress[i] >= objective.Amount;

			switch (objective.Type)
			{
				case "collect_item":
					string itemName = GameData.Instance.GetItem(objective.ItemId)?.Name ?? objective.ItemId;
					parts.Add($"{state.Progress[i]}/{objective.Amount} {itemName}");
					break;
				case "talk_to_npc":
					string npcName = GameData.Instance.GetCharacter(objective.TargetId ?? "")?.Name ?? objective.TargetId ?? "?";
					parts.Add(done ? $"Mit {npcName} gesprochen" : $"Mit {npcName} sprechen");
					break;
				case "reach_location":
					parts.Add(done ? "Ort erreicht" : "Ort aufsuchen");
					break;
				case "kill_enemy":
					string enemyName = GameData.Instance.GetCharacter(objective.TargetId ?? "")?.Name ?? objective.TargetId ?? "?";
					parts.Add($"{state.Progress[i]}/{objective.Amount} {enemyName} besiegt");
					break;
			}
		}

		return string.Join(", ", parts);
	}
}
