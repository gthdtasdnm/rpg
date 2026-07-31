using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace RPG.Data;

// Autoload: lädt beim Start alle JSON-Dateien aus Data/Characters|Items|Quests|Spells.
// Neue Inhalte hinzufügen = neue JSON-Datei ablegen, keine Code-Änderung nötig (siehe Data/README.md).
// Dialoge liegen NICHT hier, sondern als .dialogue-Dateien in Dialogues/ (Dialogue-Manager-Addon).
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
	private readonly Dictionary<string, SpellDefinition> _spells = new();

	public override void _Ready()
	{
		Instance = this;

		LoadAll("res://Data/Characters", _characters, d => d.Id);
		LoadAll("res://Data/Items", _items, d => d.Id);
		LoadAll("res://Data/Quests", _quests, d => d.Id);
		LoadAll("res://Data/Spells", _spells, d => d.Id);
	}

	public CharacterDefinition? GetCharacter(string id) => _characters.GetValueOrDefault(id);
	public ItemDefinition? GetItem(string id) => _items.GetValueOrDefault(id);

	// Im Editor laufen keine Autoloads, `Instance` ist dort also null. @tool-Scripts wie
	// ItemPickup brauchen die Item-Daten trotzdem, um schon beim Platzieren das richtige Modell
	// zu zeigen - deshalb dieser Umweg, der die JSONs notfalls selbst liest.
	public static ItemDefinition? LookupItem(string id)
	{
		if (Instance != null)
			return Instance.GetItem(id);

		if (_editorItems == null)
		{
			_editorItems = new Dictionary<string, ItemDefinition>();
			LoadAll("res://Data/Items", _editorItems, d => d.Id);
		}

		return _editorItems.GetValueOrDefault(id);
	}

	private static Dictionary<string, ItemDefinition>? _editorItems;
	public QuestDefinition? GetQuest(string id) => _quests.GetValueOrDefault(id);
	public SpellDefinition? GetSpell(string id) => _spells.GetValueOrDefault(id);

	public IEnumerable<ItemDefinition> GetAllItems() => _items.Values;
	public IEnumerable<QuestDefinition> GetAllQuests() => _quests.Values;

	private static void LoadAll<T>(string folder, Dictionary<string, T> target, System.Func<T, string> getId)
	{
		foreach (string fileName in DirAccess.GetFilesAt(folder))
		{
			// Dateien mit "_" am Anfang sind Vorlagen/Entwuerfe (siehe Data/README.md) und
			// gehoeren bewusst nicht ins Spiel.
			if (!fileName.EndsWith(".json") || fileName.StartsWith("_"))
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
