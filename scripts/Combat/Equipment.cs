using Godot;
using RPG.Data;
using RPG.Items;
using System.Collections.Generic;

namespace RPG.Combat;

// Was der Charakter angelegt hat - je ein Slot für Waffe, Schild und Rüstung.
// Manuelles Equip: der Spieler wählt im Inventar aus (siehe scripts/UI/InventoryScreen.cs).
// Sitzt als Sibling von Inventory (gleicher Parent, z.B. Player).
public partial class Equipment : Node
{
	// Slot-Namen sind identisch mit ItemDefinition.Type - dadurch braucht es keine zweite
	// Zuordnungstabelle, und ein Item weiß von sich aus, wohin es gehört.
	public const string SlotWeapon = "weapon";
	public const string SlotShield = "shield";
	public const string SlotArmor = "armor";

	public static readonly string[] Slots = { SlotWeapon, SlotShield, SlotArmor };

	[Signal] public delegate void WeaponChangedEventHandler(string itemId);
	[Signal] public delegate void ShieldChangedEventHandler(string itemId);
	[Signal] public delegate void ArmorChangedEventHandler(string itemId);

	public string? EquippedWeaponId { get; private set; }
	public string? EquippedShieldId { get; private set; }
	public string? EquippedArmorId { get; private set; }

	private Inventory? _inventory;

	public override void _Ready()
	{
		// Verkauftes, weggeworfenes oder für eine Quest abgegebenes Zeug soll nicht angelegt
		// bleiben. Statt jeden dieser Wege einzeln daran zu erinnern, prüft die Ausrüstung nach
		// jeder Inventaränderung selbst nach, ob sie noch besitzt, was sie trägt.
		_inventory = GetParent()?.GetNodeOrNull<Inventory>("Inventory");
		if (_inventory != null)
			_inventory.InventoryChanged += DropWhatIsNoLongerOwned;
	}

	// Legt ein Item in den zu seinem Type passenden Slot (weapon/shield/armor). Andere Typen
	// (material/consumable/quest) sind nicht ausrüstbar und werden ignoriert.
	public void Equip(string itemId)
	{
		ItemDefinition? item = GameData.Instance.GetItem(itemId);
		if (item == null || !item.IsEquippable)
			return;

		SetSlot(item.Type, itemId);
	}

	public void Unequip(string slot) => SetSlot(slot, null);

	public string? GetEquipped(string slot) => slot switch
	{
		SlotWeapon => EquippedWeaponId,
		SlotShield => EquippedShieldId,
		SlotArmor => EquippedArmorId,
		_ => null,
	};

	public bool IsEquipped(string itemId) =>
		!string.IsNullOrEmpty(itemId)
		&& (itemId == EquippedWeaponId || itemId == EquippedShieldId || itemId == EquippedArmorId);

	// Schaden der angelegten Waffe; ohne Waffe schlägt man mit der Faust zu (siehe
	// doc/spielsysteme.md, Abschnitt 1 - der Kampf selbst ist noch Konzept).
	public int WeaponDamage => GetDefinition(EquippedWeaponId)?.Damage ?? 1;

	public int TotalDefense => (GetDefinition(EquippedShieldId)?.Defense ?? 0)
		+ (GetDefinition(EquippedArmorId)?.Defense ?? 0);

	public int TotalStaggerResist => (GetDefinition(EquippedShieldId)?.StaggerResist ?? 0)
		+ (GetDefinition(EquippedArmorId)?.StaggerResist ?? 0);

	// Fürs Savegame (siehe SaveData.EquippedItems): Slot-Name -> Item-Id, leere Slots fehlen.
	public Dictionary<string, string> GetEquippedForSave()
	{
		Dictionary<string, string> result = new();
		foreach (string slot in Slots)
		{
			string? itemId = GetEquipped(slot);
			if (!string.IsNullOrEmpty(itemId))
				result[slot] = itemId;
		}

		return result;
	}

	public void RestoreEquipment(IReadOnlyDictionary<string, string> equipped)
	{
		foreach (string slot in Slots)
			SetSlot(slot, equipped.TryGetValue(slot, out string? itemId) ? itemId : null);
	}

	private void SetSlot(string slot, string? itemId)
	{
		switch (slot)
		{
			case SlotWeapon:
				EquippedWeaponId = itemId;
				EmitSignal(SignalName.WeaponChanged, itemId ?? "");
				break;
			case SlotShield:
				EquippedShieldId = itemId;
				EmitSignal(SignalName.ShieldChanged, itemId ?? "");
				break;
			case SlotArmor:
				EquippedArmorId = itemId;
				EmitSignal(SignalName.ArmorChanged, itemId ?? "");
				break;
		}
	}

	private void DropWhatIsNoLongerOwned()
	{
		if (_inventory == null)
			return;

		foreach (string slot in Slots)
		{
			string? itemId = GetEquipped(slot);
			if (!string.IsNullOrEmpty(itemId) && !_inventory.HasItem(itemId))
				SetSlot(slot, null);
		}
	}

	private static ItemDefinition? GetDefinition(string? itemId) =>
		string.IsNullOrEmpty(itemId) ? null : GameData.Instance.GetItem(itemId);
}
