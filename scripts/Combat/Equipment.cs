using Godot;
using RPG.Data;

namespace RPG.Combat;

// Manuelles Equip: der Spieler waehlt im Inventar-Panel per "Ausrüsten"-Knopf, was angelegt wird
// (siehe Hud.cs). Sitzt als Sibling von Inventory (gleicher Parent, z.B. Player).
public partial class Equipment : Node
{
	[Signal] public delegate void WeaponChangedEventHandler(string itemId);
	[Signal] public delegate void ShieldChangedEventHandler(string itemId);
	[Signal] public delegate void ArmorChangedEventHandler(string itemId);

	public string? EquippedWeaponId { get; private set; }
	public string? EquippedShieldId { get; private set; }
	public string? EquippedArmorId { get; private set; }

	// Legt ein Item in den zu seinem Type passenden Slot (weapon/shield/armor). Andere Typen
	// (material/consumable/quest) sind nicht ausruestbar und werden ignoriert.
	public void Equip(string itemId)
	{
		ItemDefinition? item = GameData.Instance.GetItem(itemId);
		if (item == null)
			return;

		switch (item.Type)
		{
			case "weapon":
				EquippedWeaponId = itemId;
				EmitSignal(SignalName.WeaponChanged, itemId);
				break;
			case "shield":
				EquippedShieldId = itemId;
				EmitSignal(SignalName.ShieldChanged, itemId);
				break;
			case "armor":
				EquippedArmorId = itemId;
				EmitSignal(SignalName.ArmorChanged, itemId);
				break;
		}
	}
}
