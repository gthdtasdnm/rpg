using System.Collections.Generic;

namespace RPG.World;

// Reiner Datencontainer fuers Savegame (user://savegame.json), gleiches JSON-Muster wie Data/*.
public class SaveData
{
	public float PlayerX { get; set; }
	public float PlayerY { get; set; }
	public float PlayerZ { get; set; }
	public float PlayerRotationY { get; set; }
	public int PlayerHealth { get; set; }
	public Dictionary<string, int> InventoryItems { get; set; } = new();
	public List<string> Flags { get; set; } = new();
	public Dictionary<string, int[]> ActiveQuestProgress { get; set; } = new();
}
