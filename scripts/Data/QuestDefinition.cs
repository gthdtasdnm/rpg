using System.Collections.Generic;

namespace RPG.Data;

// Objective.Type: "collect_item" (nutzt ItemId/Amount) | "talk_to_npc" (nutzt TargetId = CharacterId)
// | "reach_location" (nutzt TargetId = FlagId, gesetzt von LocationTrigger)
// | "kill_enemy" (nutzt TargetId = CharacterId des Gegners, Amount = Anzahl, ausgeloest von
// Enemy.OnDied) - weitere Typen additiv ergänzbar, siehe QuestManager.NotifyXyz.
public class QuestObjective
{
	public string Type { get; set; } = "collect_item";
	public string ItemId { get; set; } = "";
	public string? TargetId { get; set; }
	public int Amount { get; set; } = 1;
}

public class QuestDefinition
{
	public string Id { get; set; } = "";
	public string Title { get; set; } = "";
	public string? GiverNpcId { get; set; }
	public List<QuestObjective> Objectives { get; set; } = new();
	public List<string> RewardItemIds { get; set; } = new();
	public int RewardGold { get; set; } = 0;
}
