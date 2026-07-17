using Godot;
using System.Collections.Generic;

namespace RPG.Items;

public partial class Inventory : Node
{
	// ItemAdded: fuer Systeme, die auf NEUE Items reagieren muessen (z.B. QuestManager).
	// InventoryChanged: generisches "bitte neu anzeigen" fuers Inventar-UI, feuert bei jeder Aenderung.
	[Signal] public delegate void ItemAddedEventHandler(string itemId, int totalCount);
	[Signal] public delegate void InventoryChangedEventHandler();

	private readonly Dictionary<string, int> _items = new();

	public int GetCount(string itemId) => _items.GetValueOrDefault(itemId, 0);
	public bool HasItem(string itemId, int amount = 1) => GetCount(itemId) >= amount;
	public IReadOnlyDictionary<string, int> GetAllItems() => _items;

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
