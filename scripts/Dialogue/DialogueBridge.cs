using DialogueManagerRuntime;
using Godot;
using Godot.Collections;
using RPG.Data;
using RPG.Items;
using RPG.Quests;
using RPG.World;

namespace RPG.Dialogue;

// Autoload "Dialog" - die Bruecke zwischen Dialogtexten und dem restlichen Spiel.
//
// In einer .dialogue-Datei ruft man diese Methoden direkt auf:
//     do Dialog.SetFlag("kennt_die_barriere")
//     if Dialog.HasFlag("kennt_die_barriere")
//
// Warum eine eigene Klasse, statt in den Dialogen direkt GameFlags/QuestManager anzusprechen:
//  1. Ein Ort, an dem steht, was Dialoge duerfen - beim Dialogschreiben muss man sich nicht
//     merken, welcher Autoload welche Methode hat.
//  2. Der Rest des Spiels erfaehrt hier, ob gerade geredet wird (IsActive/Signale) - Player und
//     HUD haengen daran, ohne das Dialog-Addon kennen zu muessen.
//  3. Falls das Addon (GDScript) C#-Methoden mal nicht aufloesen kann, muss nur DIESE Klasse
//     durch einen GDScript-Adapter ersetzt werden, nicht jeder einzelne Dialog.
public partial class DialogueBridge : Node
{
	public static DialogueBridge Instance { get; private set; } = null!;

	[Signal] public delegate void DialogueStartedEventHandler();
	[Signal] public delegate void DialogueEndedEventHandler();

	// Der Dialog fordert das Haendler-Fenster an (Hud.cs haengt daran). Der Dialog laeuft dabei
	// weiter - das Shop-Panel legt sich einfach darueber.
	[Signal] public delegate void ShopRequestedEventHandler(string characterId);

	// Laeuft gerade ein Gespraech? Player (Bewegung/Maus) und HUD (Tastenkuerzel) fragen das ab.
	public bool IsActive { get; private set; }

	public override void _Ready()
	{
		Instance = this;

		// Erster Zugriff auf .Instance verbindet die GDScript-Signale des Addons mit den
		// statischen C#-Events (siehe addons/dialogue_manager/DialogueManager.cs).
		_ = DialogueManager.Instance;
		DialogueManager.DialogueStarted += OnAddonDialogueStarted;
		DialogueManager.DialogueEnded += OnAddonDialogueEnded;
	}

	public override void _ExitTree()
	{
		DialogueManager.DialogueStarted -= OnAddonDialogueStarted;
		DialogueManager.DialogueEnded -= OnAddonDialogueEnded;
	}

	private void OnAddonDialogueStarted(Resource dialogueResource)
	{
		IsActive = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		EmitSignal(SignalName.DialogueStarted);
	}

	private void OnAddonDialogueEnded(Resource dialogueResource)
	{
		IsActive = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		EmitSignal(SignalName.DialogueEnded);
	}

	// GDScript kann C#-Properties nicht immer lesen, Methoden dagegen zuverlaessig aufrufen.
	public bool IsDialogueActive() => IsActive;

	// Einziger Weg, ein Gespraech zu starten (Npc, ProximityDialogue, GuardCheckpoint nutzen ihn).
	// dialogueFile = Pfad auf eine .dialogue-Datei, title = "~ titel"-Marke darin.
	// speaker wird dem Dialog als zusaetzlicher "game state" mitgegeben, sodass die Datei auf die
	// Felder des sprechenden Knotens zugreifen kann.
	public void Show(string dialogueFile, string title = "start", Node? speaker = null)
	{
		if (string.IsNullOrEmpty(dialogueFile))
		{
			GD.PushWarning("Dialog.Show: keine Dialogdatei angegeben.");
			return;
		}

		Resource? dialogue = ResourceLoader.Load<Resource>(dialogueFile);
		if (dialogue == null)
		{
			GD.PushError($"Dialog.Show: '{dialogueFile}' konnte nicht geladen werden.");
			return;
		}

		Array<Variant> extraGameStates = new();
		if (speaker != null)
			extraGameStates.Add(speaker);

		DialogueManager.ShowDialogueBalloon(dialogue, title, extraGameStates);
	}

	// ---------------------------------------------------------------------------------------
	// Ab hier: alles, was in .dialogue-Dateien benutzt werden kann.
	// ---------------------------------------------------------------------------------------

	// Weltzustand merken - z.B. dass der Spieler etwas erfahren hat oder jemandem begegnet ist.
	// Namenskonventionen fuer Flags siehe scripts/World/GameFlags.cs.
	public void SetFlag(string flagId) => GameFlags.Instance.SetFlag(flagId);

	public void ClearFlag(string flagId) => GameFlags.Instance.SetFlag(flagId, false);

	public bool HasFlag(string flagId) => GameFlags.Instance.HasFlag(flagId);

	// Quest aus Data/Quests starten. Unbekannte Id = Fehlermeldung, kein Absturz.
	public void StartQuest(string questId) => QuestManager.Instance.StartQuest(questId);

	public void CompleteQuest(string questId) => QuestManager.Instance.CompleteQuest(questId);

	public bool IsQuestStarted(string questId) => GameFlags.Instance.HasFlag($"quest_started_{questId}");

	public bool IsQuestCompleted(string questId) => GameFlags.Instance.HasFlag($"quest_completed_{questId}");

	// Alle Ziele erfuellt, aber noch nicht beim Auftraggeber abgegeben - genau der Zustand, in
	// dem eine "Ich hab's erledigt"-Antwort im Dialog auftauchen soll.
	public bool IsQuestReady(string questId) => GameFlags.Instance.HasFlag($"quest_ready_{questId}");

	// Oeffnet das Haendlerfenster fuer diesen Charakter (Sortiment: CharacterDefinition.ShopItemIds).
	public void OpenShop(string characterId) => EmitSignal(SignalName.ShopRequested, characterId);

	// --- Inventar & Silber ---------------------------------------------------------------
	// Waehrung des Spiels ist Silber (eine einzige Waehrung, siehe Inventory.cs). Preise stehen
	// als "price" in den Item-JSONs.

	public bool HasItem(string itemId, int amount = 1) => PlayerInventory?.HasItem(itemId, amount) ?? false;

	public int CountItem(string itemId) => PlayerInventory?.GetCount(itemId) ?? 0;

	public void GiveItem(string itemId, int amount = 1) => PlayerInventory?.AddItem(itemId, amount);

	// Gibt false zurueck, wenn der Spieler die Sachen gar nicht hat - im Dialog also immer
	// vorher mit HasItem pruefen, wenn die Antwort davon abhaengen soll.
	public bool TakeItem(string itemId, int amount = 1) => PlayerInventory?.RemoveItem(itemId, amount) ?? false;

	public int GetSilver() => PlayerInventory?.Silver ?? 0;

	public bool HasSilver(int amount) => GetSilver() >= amount;

	public void GiveSilver(int amount) => PlayerInventory?.AddSilver(amount);

	public bool TakeSilver(int amount) => PlayerInventory?.SpendSilver(amount) ?? false;

	// --- Lehrer / Faehigkeiten -----------------------------------------------------------
	// Eine Faehigkeit ist ein Item vom Typ "skill" in Data/Items (dort steht auch der Preis).
	// Gelernt wird nicht ins Inventar, sondern als Flag "learned_<id>".

	public bool HasLearned(string skillId) => GameFlags.Instance.HasFlag($"learned_{skillId}");

	public int GetPrice(string itemId) => GameData.Instance.GetItem(itemId)?.Price ?? 0;

	// Zieht den Preis aus den Item-Daten ab und setzt das Lern-Flag. false = zu wenig Silber,
	// schon gelernt, oder unbekannte Id. Im Dialog davor mit HasSilver/HasLearned pruefen,
	// damit der NPC passend reagieren kann.
	public bool Learn(string skillId)
	{
		if (HasLearned(skillId))
			return false;

		ItemDefinition? skill = GameData.Instance.GetItem(skillId);
		if (skill == null)
		{
			GD.PushError($"Dialog.Learn: unbekannte Faehigkeit '{skillId}' (fehlt Data/Items/{skillId}.json?)");
			return false;
		}

		if (!TakeSilver(skill.Price))
			return false;

		GameFlags.Instance.SetFlag($"learned_{skillId}");
		return true;
	}

	private Inventory? PlayerInventory
	{
		get
		{
			Node? player = GetTree().GetFirstNodeInGroup("player");
			return player?.GetNodeOrNull<Inventory>("Inventory");
		}
	}
}
