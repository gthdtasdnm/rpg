using System.Collections.Generic;

namespace RPG.Data;

public class DialogueChoice
{
	public string Text { get; set; } = "";
	public string? Next { get; set; }
	public string? StartQuest { get; set; }
	public string? CompleteQuest { get; set; }
	public string? SetFlag { get; set; }
	public string? RequiresFlag { get; set; }
	public string? RequiresNotFlag { get; set; }
}

public class DialogueNode
{
	public string Text { get; set; } = "";
	public List<DialogueChoice> Choices { get; set; } = new();
}

// Wird beim Dialogstart der Reihe nach geprueft; die erste Regel, deren Bedingung zutrifft,
// bestimmt den Einstiegsknoten (z.B. andere Begruessung, wenn eine Quest schon laeuft/fertig ist).
public class DialogueStartRule
{
	public string? RequiresFlag { get; set; }
	public string? RequiresNotFlag { get; set; }
	public string Node { get; set; } = "";
}

public class DialogueDefinition
{
	public string Id { get; set; } = "";
	public string StartNode { get; set; } = "start";
	public List<DialogueStartRule> StartRules { get; set; } = new();
	public Dictionary<string, DialogueNode> Nodes { get; set; } = new();
}
