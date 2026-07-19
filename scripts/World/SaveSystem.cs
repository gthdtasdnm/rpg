using Godot;
using RPG.Characters;
using RPG.Items;
using RPG.Quests;
using System.Text.Json;

namespace RPG.World;

// Autoload: schreibt/liest ein einzelnes Savegame nach user://savegame.json.
// Sammelt/verteilt Zustand von Player, Inventory, GameFlags und QuestManager - selbst
// zustandslos, kennt nur die Reihenfolge in der die anderen Systeme befragt werden.
public partial class SaveSystem : Node
{
	public static SaveSystem Instance { get; private set; } = null!;

	private const string SavePath = "user://savegame.json";

	public override void _Ready()
	{
		Instance = this;
	}

	public bool HasSaveGame() => FileAccess.FileExists(SavePath);

	public bool Save()
	{
		Player? player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return false;

		CharacterStats stats = player.GetNode<CharacterStats>("Stats");
		Inventory inventory = player.GetNode<Inventory>("Inventory");

		SaveData data = new()
		{
			PlayerX = player.GlobalPosition.X,
			PlayerY = player.GlobalPosition.Y,
			PlayerZ = player.GlobalPosition.Z,
			PlayerRotationY = player.Rotation.Y,
			PlayerHealth = stats.CurrentHealth,
			Gold = inventory.Gold,
			InventoryItems = new(inventory.GetAllItems()),
			Flags = new(GameFlags.Instance.GetAllFlags()),
			ActiveQuestProgress = QuestManager.Instance.GetActiveQuestProgressForSave(),
		};

		string json = JsonSerializer.Serialize(data);

		using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError($"SaveSystem: konnte {SavePath} nicht schreiben ({FileAccess.GetOpenError()})");
			return false;
		}

		file.StoreString(json);
		return true;
	}

	public bool Load()
	{
		if (!HasSaveGame())
			return false;

		Player? player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return false;

		using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError($"SaveSystem: konnte {SavePath} nicht lesen ({FileAccess.GetOpenError()})");
			return false;
		}

		SaveData? data = JsonSerializer.Deserialize<SaveData>(file.GetAsText());
		if (data == null)
			return false;

		player.ApplySaveState(new Vector3(data.PlayerX, data.PlayerY, data.PlayerZ), data.PlayerRotationY);

		player.GetNode<CharacterStats>("Stats").RestoreHealth(data.PlayerHealth);
		Inventory playerInventory = player.GetNode<Inventory>("Inventory");
		playerInventory.LoadItems(data.InventoryItems);
		playerInventory.RestoreGold(data.Gold);
		GameFlags.Instance.LoadFlags(data.Flags);
		QuestManager.Instance.RestoreActiveQuests(data.ActiveQuestProgress);

		return true;
	}
}
