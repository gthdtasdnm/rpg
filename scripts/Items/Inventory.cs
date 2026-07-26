using Godot;
using RPG.Quests;
using System.Collections.Generic;

namespace RPG.Items;

public partial class Inventory : Node
{
	// ItemAdded: fuer Systeme, die auf NEUE Items reagieren muessen (z.B. QuestManager).
	// InventoryChanged: generisches "bitte neu anzeigen" fuers Inventar-UI, feuert bei jeder Aenderung.
	[Signal] public delegate void ItemAddedEventHandler(string itemId, int totalCount);
	[Signal] public delegate void InventoryChangedEventHandler();
	[Signal] public delegate void SilverChangedEventHandler(int totalSilver);

	// Startausruestung, im Inspector einstellbar. Wird beim Spielstart einmal eingelegt - fuer
	// mehrere Exemplare dieselbe Id mehrfach eintragen. Aktuell nur zum Ausprobieren gefuellt
	// (siehe World/player.tscn); fuer den echten Spielanfang hier wieder leeren.
	[Export] public string[] StartItems = System.Array.Empty<string>();

	[Export] public int StartSilver;

	private readonly Dictionary<string, int> _items = new();

	// Waehrung des Spiels ist **Silber** - eine einzige Waehrung, keine Kupfer/Gold-Staffelung.
	// Preise stehen als ItemDefinition.Price in den Item-JSONs. Bewusst kein Item-Stack, kein
	// Gewicht/Slot-Konzept noetig fuer ein reines Zahlenkonto.
	public int Silver { get; private set; }

	public override void _Ready()
	{
		// Verzoegert: erst wenn der ganze Baum steht, ist der Spieler in seiner Gruppe und das
		// HUD haengt an den Signalen.
		CallDeferred(nameof(ApplyStartingGear));
	}

	private void ApplyStartingGear()
	{
		// Sammel-Quests brauchen die Meldung, dass etwas aufgenommen wurde. Frueher hat das der
		// C#-Player verdrahtet - der aktive Spieler ist GDScript, deshalb macht das Inventar es
		// selbst, sobald es zu einem Spieler gehoert.
		if (GetParent()?.IsInGroup("player") == true)
			ItemAdded += (itemId, totalCount) => QuestManager.Instance.NotifyItemCollected(itemId, totalCount);

		foreach (string itemId in StartItems)
		{
			if (!string.IsNullOrWhiteSpace(itemId))
				AddItem(itemId);
		}

		if (StartSilver > 0)
			AddSilver(StartSilver);
	}

	public int GetCount(string itemId) => _items.GetValueOrDefault(itemId, 0);
	public bool HasItem(string itemId, int amount = 1) => GetCount(itemId) >= amount;
	public IReadOnlyDictionary<string, int> GetAllItems() => _items;

	public void AddSilver(int amount)
	{
		if (amount <= 0)
			return;

		Silver += amount;
		EmitSignal(SignalName.SilverChanged, Silver);
	}

	public bool SpendSilver(int amount)
	{
		if (amount <= 0 || Silver < amount)
			return false;

		Silver -= amount;
		EmitSignal(SignalName.SilverChanged, Silver);
		return true;
	}

	public void RestoreSilver(int amount)
	{
		Silver = Mathf.Max(0, amount);
		EmitSignal(SignalName.SilverChanged, Silver);
	}

	public void AddItem(string itemId, int amount = 1)
	{
		if (amount <= 0)
			return;

		int total = GetCount(itemId) + amount;
		_items[itemId] = total;
		EmitSignal(SignalName.ItemAdded, itemId, total);
		EmitSignal(SignalName.InventoryChanged);
	}

	public bool RemoveItem(string itemId, int amount = 1)
	{
		if (!HasItem(itemId, amount))
			return false;

		_items[itemId] -= amount;
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public void LoadItems(IReadOnlyDictionary<string, int> items)
	{
		_items.Clear();
		foreach (KeyValuePair<string, int> entry in items)
			_items[entry.Key] = entry.Value;

		EmitSignal(SignalName.InventoryChanged);
	}
}
