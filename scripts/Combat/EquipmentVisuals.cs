using Godot;
using RPG.Data;
using RPG.Items;

namespace RPG.Combat;

// Zeigt am Charakter, was er angelegt hat: hängt als Node3D unter dem Spieler (siehe
// World/player.tscn, Knoten "WeaponMount") und tauscht sein Kindmodell, sobald sich der
// zugeordnete Ausrüstungsslot ändert.
//
// Der Spieler ist derzeit eine Kapsel ohne Skelett - die Waffe hängt deshalb an einem frei
// verschiebbaren Punkt statt an einem Handknochen. Position, Drehung und `ModelRotationDegrees`
// sind im Inspector einstellbar; sobald es ein echtes Charaktermodell gibt, wandert dieser Knoten
// an den passenden BoneAttachment3D und der Rest bleibt, wie er ist.
public partial class EquipmentVisuals : Node3D
{
	[Export(PropertyHint.Enum, "weapon,shield,armor")]
	public string Slot = Equipment.SlotWeapon;

	// Wie das Modell relativ zum Aufhängepunkt liegt. Voreinstellung: quer über dem Rücken.
	[Export] public Vector3 ModelRotationDegrees = new(12f, 0f, 35f);

	private Equipment? _equipment;
	private Node3D? _current;

	public override void _Ready()
	{
		_equipment = GetParent()?.GetNodeOrNull<Equipment>("Equipment");
		if (_equipment == null)
		{
			GD.PushWarning($"EquipmentVisuals ({Name}): kein Equipment-Knoten neben dem Elternknoten gefunden");
			return;
		}

		switch (Slot)
		{
			case Equipment.SlotWeapon:
				_equipment.WeaponChanged += Show;
				break;
			case Equipment.SlotShield:
				_equipment.ShieldChanged += Show;
				break;
			case Equipment.SlotArmor:
				_equipment.ArmorChanged += Show;
				break;
		}

		Show(_equipment.GetEquipped(Slot) ?? "");
	}

	private void Show(string itemId)
	{
		if (_current != null)
		{
			RemoveChild(_current);
			_current.QueueFree();
			_current = null;
		}

		ItemDefinition? item = GameData.LookupItem(itemId);
		if (item == null)
			return;

		_current = ItemModel.Instantiate(item);
		if (_current == null)
			return;

		_current.RotationDegrees = ModelRotationDegrees;
		AddChild(_current);
	}
}
