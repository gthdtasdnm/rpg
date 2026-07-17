using System.Collections.Generic;

namespace RPG.Data;

// Objective.Type: aktuell nur "collect_item" ausgewertet, weitere Typen additiv ergänzbar
public class QuestObjective
{
	public string Type { get; set; } = "collect_item";
	public string ItemId { get; set; } = "";
	public int Amount { get; set; } = 1;
}

public class QuestDefinition
{
	public string Id { get; set; } = "";
	public string Title { get; set; } = "";
	public string? GiverNpcId { get; set; }
	public List<QuestObjective> Objectives { get; set; } = new();
	public List<string> RewardItemIds { get; set; } = new();
}
