using Godot;
using RPG.Characters;
using RPG.Combat;
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
		// Bewusst Node3D statt der C#-Player-Klasse: der aktive Spieler ist ein GDScript-Node
		// (scripts/World/player.gd). Gebraucht werden nur Position/Drehung und die
		// Kind-Knoten Stats/Inventory - beides sprachunabhaengig.
		Node3D? player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		if (player == null)
			return false;

		CharacterStats stats = player.GetNode<CharacterStats>("Stats");
		Inventory inventory = player.GetNode<Inventory>("Inventory");
		Equipment equipment = player.GetNode<Equipment>("Equipment");

		SaveData data = new()
		{
			PlayerX = player.GlobalPosition.X,
			PlayerY = player.GlobalPosition.Y,
			PlayerZ = player.GlobalPosition.Z,
			PlayerRotationY = player.Rotation.Y,
			PlayerHealth = stats.CurrentHealth,
			Silver = inventory.Silver,
			InventoryItems = new(inventory.GetAllItems()),
			EquippedItems = equipment.GetEquippedForSave(),
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

		// Bewusst Node3D statt der C#-Player-Klasse: der aktive Spieler ist ein GDScript-Node
		// (scripts/World/player.gd). Gebraucht werden nur Position/Drehung und die
		// Kind-Knoten Stats/Inventory - beides sprachunabhaengig.
		Node3D? player = GetTree().GetFirstNodeInGroup("player") as Node3D;
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

		// Der Player zieht beim Setzen der Position auch seinen internen Kamera-Yaw nach, sonst
		// springt die Kamera nach dem Laden zurueck. Fehlt die Methode, wenigstens hart setzen.
		Vector3 savedPosition = new(data.PlayerX, data.PlayerY, data.PlayerZ);
		if (player.HasMethod("apply_save_state"))
		{
			player.Call("apply_save_state", savedPosition, data.PlayerRotationY);
		}
		else
		{
			player.GlobalPosition = savedPosition;
			player.Rotation = new Vector3(0f, data.PlayerRotationY, 0f);
		}

		player.GetNode<CharacterStats>("Stats").RestoreHealth(data.PlayerHealth);
		Inventory playerInventory = player.GetNode<Inventory>("Inventory");
		playerInventory.LoadItems(data.InventoryItems);
		playerInventory.RestoreSilver(data.Silver);

		// Muss nach dem Inventar kommen: Equipment legt beim Laden erst einmal alles ab, was im
		// gespeicherten Rucksack nicht vorkommt (siehe Equipment.DropWhatIsNoLongerOwned).
		player.GetNode<Equipment>("Equipment").RestoreEquipment(data.EquippedItems);
		GameFlags.Instance.LoadFlags(data.Flags);
		QuestManager.Instance.RestoreActiveQuests(data.ActiveQuestProgress);

		return true;
	}
}
