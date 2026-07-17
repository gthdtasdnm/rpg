using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace RPG.Data;

// Autoload: lädt beim Start alle JSON-Dateien aus Data/Characters|Items|Quests|Dialogues.
// Neue Inhalte hinzufügen = neue JSON-Datei ablegen, kein Code-Änderung nötig.
public partial class GameData : Node
{
	public static GameData Instance { get; private set; } = null!;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
	};

	private readonly Dictionary<string, CharacterDefinition> _characters = new();
	private readonly Dictionary<string, ItemDefinition> _items = new();
	private readonly Dictionary<string, QuestDefinition> _quests = new();
	private readonly Dictionary<string, DialogueDefinition> _dialogues = new();

	public override void _Ready()
	{
		Instance = this;

		LoadAll("res://Data/Characters", _characters, d => d.Id);
		LoadAll("res://Data/Items", _items, d => d.Id);
		LoadAll("res://Data/Quests", _quests, d => d.Id);
		LoadAll("res://Data/Dialogues", _dialogues, d => d.Id);
	}

	public CharacterDefinition? GetCharacter(string id) => _characters.GetValueOrDefault(id);
	public ItemDefinition? GetItem(string id) => _items.GetValueOrDefault(id);
	public QuestDefinition? GetQuest(string id) => _quests.GetValueOrDefault(id);
	public DialogueDefinition? GetDialogue(string id) => _dialogues.GetValueOrDefault(id);

	public IEnumerable<ItemDefinition> GetAllItems() => _items.Values;
	public IEnumerable<QuestDefinition> GetAllQuests() => _quests.Values;

	private static void LoadAll<T>(string folder, Dictionary<string, T> target, System.Func<T, string> getId)
	{
		foreach (string fileName in DirAccess.GetFilesAt(folder))
		{
			if (!fileName.EndsWith(".json"))
				continue;

			string path = $"{folder}/{fileName}";
			using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PushError($"GameData: konnte {path} nicht öffnen ({FileAccess.GetOpenError()})");
				continue;
			}

			string json = file.GetAsText();
			T? entry = JsonSerializer.Deserialize<T>(json, JsonOptions);
			if (entry == null)
			{
				GD.PushError($"GameData: konnte {path} nicht als {typeof(T).Name} lesen");
				continue;
			}

			target[getId(entry)] = entry;
		}
	}
}
