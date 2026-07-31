using Godot;
using RPG.Characters;
using RPG.Combat;
using RPG.Data;
using RPG.Items;
using System.Collections.Generic;
using System.Text;

namespace RPG.UI;

// Das Inventar als anklickbares Raster (UI/InventoryScreen.tscn).
//
// Aufbau von links nach rechts: was man trägt, was man besitzt, was das Ausgewählte kann.
// Ein Klick im Raster wählt aus und füllt die Detailspalte, die Knöpfe darunter sind die
// Aktionen zum ausgewählten Item; ein Doppelklick nimmt die obere Aktion direkt.
//
// Der Schirm hängt selbst an Inventory und Equipment und frischt sich auf, wenn sich etwas
// ändert - das HUD muss ihn nur ein- und ausblenden.
public partial class InventoryScreen : PanelContainer
{
	private const string WorldItemScenePath = "res://Objects/world_item.tscn";

	// Wie weit vor dem Spieler Weggeworfenes landet.
	private const float DropDistance = 1.4f;

	private static readonly string[] CategoryLabels = { "Alle", "Waffen", "Rüstung", "Verbrauch", "Quest", "Sonstiges" };

	private Label _silverLabel = null!;
	private HBoxContainer _tabsBox = null!;
	private ScrollContainer _gridScroll = null!;
	private GridContainer _grid = null!;
	private VBoxContainer _equipSlotsBox = null!;
	private Label _equipSummary = null!;
	private Label _detailName = null!;
	private Label _detailType = null!;
	private Label _detailStats = null!;
	private Label _detailDescription = null!;
	private Button _primaryButton = null!;
	private Button _secondaryButton = null!;

	private Node3D? _player;
	private Inventory? _inventory;
	private Equipment? _equipment;
	private CharacterStats? _stats;

	private readonly Dictionary<string, ItemSlotButton> _equipSlots = new();
	private readonly Dictionary<string, Label> _equipSlotNames = new();
	private readonly ButtonGroup _slotGroup = new();
	private readonly ButtonGroup _tabGroup = new();

	private int _activeCategory;
	private string _selectedItemId = "";

	public override void _Ready()
	{
		_silverLabel = GetNode<Label>("Margin/Root/Header/SilverLabel");
		_tabsBox = GetNode<HBoxContainer>("Margin/Root/Tabs");
		_gridScroll = GetNode<ScrollContainer>("Margin/Root/Body/GridPanel/GridMargin/GridScroll");
		_grid = GetNode<GridContainer>("Margin/Root/Body/GridPanel/GridMargin/GridScroll/Grid");
		_equipSlotsBox = GetNode<VBoxContainer>("Margin/Root/Body/EquipColumn/EquipSlots");
		_equipSummary = GetNode<Label>("Margin/Root/Body/EquipColumn/EquipSummary");

		const string detailPath = "Margin/Root/Body/DetailPanel/DetailMargin/Detail";
		_detailName = GetNode<Label>($"{detailPath}/NameLabel");
		_detailType = GetNode<Label>($"{detailPath}/TypeLabel");
		_detailStats = GetNode<Label>($"{detailPath}/StatsLabel");
		_detailDescription = GetNode<Label>($"{detailPath}/DescriptionLabel");
		_primaryButton = GetNode<Button>($"{detailPath}/Actions/PrimaryButton");
		_secondaryButton = GetNode<Button>($"{detailPath}/Actions/SecondaryButton");

		_primaryButton.Pressed += RunPrimaryAction;
		_secondaryButton.Pressed += DropSelected;
		_gridScroll.Resized += UpdateColumnCount;

		BuildTabs();
		BuildEquipSlots();

		// Verzoegert: der Spieler traegt sich erst in seinem eigenen _ready in die Gruppe "player"
		// ein (scripts/World/player.gd) und steht in Main.tscn hinter dem HUD. Wird hier direkt
		// gesucht, ist er noch nicht da und das Raster bliebe fuer immer leer.
		CallDeferred(nameof(ConnectToPlayer));
	}

	private void ConnectToPlayer()
	{
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		if (_player == null)
		{
			GD.PushWarning("InventoryScreen: kein Knoten in der Gruppe 'player' gefunden");
			return;
		}

		_stats = _player.GetNodeOrNull<CharacterStats>("Stats");

		_inventory = _player.GetNodeOrNull<Inventory>("Inventory");
		if (_inventory != null)
		{
			_inventory.InventoryChanged += RefreshIfVisible;
			_inventory.SilverChanged += (_) => RefreshIfVisible();
		}

		_equipment = _player.GetNodeOrNull<Equipment>("Equipment");
		if (_equipment != null)
		{
			_equipment.WeaponChanged += (_) => RefreshIfVisible();
			_equipment.ShieldChanged += (_) => RefreshIfVisible();
			_equipment.ArmorChanged += (_) => RefreshIfVisible();
		}

		Refresh();
	}

	private void BuildTabs()
	{
		for (int i = 0; i < CategoryLabels.Length; i++)
		{
			int category = i;
			Button tab = new()
			{
				Text = CategoryLabels[i],
				ToggleMode = true,
				ButtonGroup = _tabGroup,
				ButtonPressed = i == 0,
			};
			tab.Pressed += () => SetCategory(category);
			_tabsBox.AddChild(tab);
		}
	}

	// Die drei Ausrüstungsslots sind feste Felder - sie stehen auch dann da, wenn nichts drin
	// liegt, damit man sieht, was noch fehlt.
	private void BuildEquipSlots()
	{
		foreach (string slot in Equipment.Slots)
		{
			HBoxContainer row = new();
			row.AddThemeConstantOverride("separation", 8);

			// Gleiche Auswahlgruppe wie die Rasterfelder: es ist immer genau ein Item ausgewählt,
			// egal ob es im Rucksack liegt oder getragen wird.
			ItemSlotButton slotButton = new() { ButtonGroup = _slotGroup };
			slotButton.Pressed += () => SelectSlotContents(slot);
			slotButton.Activated += () => _equipment?.Unequip(slot);
			row.AddChild(slotButton);

			VBoxContainer labels = new() { SizeFlagsVertical = SizeFlags.ShrinkCenter };
			Label slotName = new() { Text = SlotLabel(slot) };
			slotName.AddThemeFontSizeOverride("font_size", 13);
			slotName.AddThemeColorOverride("font_color", new Color(0.58f, 0.55f, 0.48f));
			labels.AddChild(slotName);

			Label itemName = new() { Text = "—", AutowrapMode = TextServer.AutowrapMode.WordSmart };
			itemName.AddThemeFontSizeOverride("font_size", 14);
			itemName.CustomMinimumSize = new Vector2(104, 0);
			labels.AddChild(itemName);

			row.AddChild(labels);
			_equipSlotsBox.AddChild(row);
			_equipSlots[slot] = slotButton;
			_equipSlotNames[slot] = itemName;
		}
	}

	public void Refresh()
	{
		_silverLabel.Text = $"Silber: {_inventory?.Silver ?? 0}";

		// Was weder im Rucksack noch am Körper ist, kann auch nicht mehr ausgewählt sein.
		if (_selectedItemId.Length > 0 && _inventory?.HasItem(_selectedItemId) != true
			&& _equipment?.IsEquipped(_selectedItemId) != true)
		{
			_selectedItemId = "";
		}

		RefreshEquipSlots();
		RefreshGrid();
		RefreshDetail();
	}

	private void RefreshIfVisible()
	{
		if (Visible)
			Refresh();
	}

	private void RefreshEquipSlots()
	{
		foreach (string slot in Equipment.Slots)
		{
			string? itemId = _equipment?.GetEquipped(slot);
			ItemDefinition? item = string.IsNullOrEmpty(itemId) ? null : GameData.Instance.GetItem(itemId);

			_equipSlots[slot].SetItem(item, 1, item != null);
			_equipSlots[slot].ButtonPressed = item != null && item.Id == _selectedItemId;
			_equipSlots[slot].TooltipText = item != null ? $"{item.Name} — Doppelklick legt ab" : "leer";
			_equipSlotNames[slot].Text = item?.Name ?? "—";
		}

		_equipSummary.Text = _equipment == null
			? ""
			: $"Schaden {_equipment.WeaponDamage}\nSchutz {_equipment.TotalDefense}\nStagger {_equipment.TotalStaggerResist}";
	}

	private void RefreshGrid()
	{
		foreach (Node child in _grid.GetChildren())
		{
			_grid.RemoveChild(child);
			child.QueueFree();
		}

		UpdateColumnCount();

		if (_inventory == null)
			return;

		foreach (KeyValuePair<string, int> entry in _inventory.GetAllItems())
		{
			if (entry.Value <= 0)
				continue;

			ItemDefinition? item = GameData.Instance.GetItem(entry.Key);

			// "skill" ist ein Trainings-Eintrag, kein Gegenstand (siehe ItemDefinition) - er darf
			// gar nicht erst im Rucksack landen und wird hier zur Sicherheit übersprungen.
			if (item == null || item.Type == "skill" || !MatchesCategory(_activeCategory, item))
				continue;

			string itemId = entry.Key;
			ItemSlotButton slot = new() { ButtonGroup = _slotGroup };
			_grid.AddChild(slot);
			slot.SetItem(item, entry.Value, _equipment?.IsEquipped(itemId) == true);
			slot.ButtonPressed = itemId == _selectedItemId;
			slot.Pressed += () => Select(itemId);
			slot.Activated += RunPrimaryAction;
		}
	}

	// Die Spaltenzahl richtet sich nach der Breite, damit das Raster in jedem Fenster gefüllt
	// aussieht statt eine halbleere Spalte übrig zu lassen.
	private void UpdateColumnCount()
	{
		const int scrollbarAllowance = 16;
		float available = _gridScroll.Size.X - scrollbarAllowance;
		int fitting = Mathf.FloorToInt(available / (ItemSlotButton.SlotSize + 8));
		_grid.Columns = Mathf.Clamp(fitting, 3, 12);
	}

	private void RefreshDetail()
	{
		ItemDefinition? item = _selectedItemId.Length > 0 ? GameData.Instance.GetItem(_selectedItemId) : null;
		if (item == null)
		{
			_detailName.Text = "Nichts ausgewählt";
			_detailType.Text = "";
			_detailStats.Text = "";
			_detailDescription.Text = "";
			_primaryButton.Visible = false;
			_secondaryButton.Visible = false;
			return;
		}

		bool isEquipped = _equipment?.IsEquipped(item.Id) == true;

		_detailName.Text = item.Name;
		_detailType.Text = isEquipped ? $"{DescribeType(item)} · angelegt" : DescribeType(item);
		_detailStats.Text = DescribeStats(item);
		_detailDescription.Text = item.Description;

		if (item.IsEquippable)
		{
			_primaryButton.Visible = true;
			_primaryButton.Text = isEquipped ? "Ablegen" : "Ausrüsten";
		}
		else if (item.Type == "consumable" && item.HealAmount > 0)
		{
			_primaryButton.Visible = true;
			_primaryButton.Text = "Benutzen";
		}
		else
		{
			_primaryButton.Visible = false;
		}

		// Questgegenstände wegzuwerfen würde die Questreihe blockieren (siehe
		// doc/Kapitelverlauf.txt - Brief, Buch, Manuskript, Fragmente).
		_secondaryButton.Visible = item.Type != "quest";
	}

	private void Select(string itemId)
	{
		_selectedItemId = itemId;
		RefreshDetail();
	}

	private void SelectSlotContents(string slot)
	{
		string? itemId = _equipment?.GetEquipped(slot);
		Select(itemId ?? "");
	}

	private void SetCategory(int category)
	{
		_activeCategory = category;
		RefreshGrid();
	}

	private void RunPrimaryAction()
	{
		ItemDefinition? item = _selectedItemId.Length > 0 ? GameData.Instance.GetItem(_selectedItemId) : null;
		if (item == null || _equipment == null)
			return;

		if (item.IsEquippable)
		{
			if (_equipment.IsEquipped(item.Id))
				_equipment.Unequip(item.Type);
			else
				_equipment.Equip(item.Id);
		}
		else if (item.Type == "consumable" && item.HealAmount > 0)
		{
			_stats?.Heal(item.HealAmount);
			_inventory?.RemoveItem(item.Id);
		}
	}

	// Weggeworfenes verschwindet nicht, sondern liegt vor dem Spieler und lässt sich wieder
	// aufheben - dieselbe Szene, die auch für von Hand platzierte Items benutzt wird.
	private void DropSelected()
	{
		ItemDefinition? item = _selectedItemId.Length > 0 ? GameData.Instance.GetItem(_selectedItemId) : null;
		if (item == null || item.Type == "quest" || _inventory == null || _player == null)
			return;

		if (!_inventory.RemoveItem(item.Id))
			return;

		PackedScene? scene = ResourceLoader.Load<PackedScene>(WorldItemScenePath);
		if (scene?.Instantiate() is not ItemPickup pickup)
		{
			GD.PushError($"InventoryScreen: {WorldItemScenePath} fehlt oder hat kein ItemPickup-Script");
			return;
		}

		pickup.ItemId = item.Id;
		pickup.Amount = 1;

		Node parent = GetTree().CurrentScene ?? _player.GetParent();
		parent.AddChild(pickup);
		pickup.GlobalPosition = _player.GlobalPosition
			+ (-_player.GlobalTransform.Basis.Z * DropDistance)
			+ (Vector3.Up * 0.2f);
	}

	private static bool MatchesCategory(int category, ItemDefinition item) => category switch
	{
		1 => item.Type == "weapon",
		2 => item.Type is "armor" or "shield",
		3 => item.Type == "consumable",
		4 => item.Type == "quest",
		5 => item.Type is not ("weapon" or "armor" or "shield" or "consumable" or "quest"),
		_ => true,
	};

	private static string SlotLabel(string slot) => slot switch
	{
		Equipment.SlotWeapon => "Waffe",
		Equipment.SlotShield => "Schild",
		Equipment.SlotArmor => "Rüstung",
		_ => slot,
	};

	private static string DescribeType(ItemDefinition item) => item.Type switch
	{
		"weapon" => $"Waffe · {DescribeWeaponCategory(item.WeaponCategory)}",
		"shield" => "Schild",
		"armor" => "Rüstung",
		"consumable" => "Verbrauchsgut",
		"quest" => "Questgegenstand",
		_ => "Material",
	};

	private static string DescribeWeaponCategory(string? category) => category switch
	{
		"twohand" => "Zweihand",
		"bow" => "Bogen",
		_ => "Einhand",
	};

	private static string DescribeDamageType(string? damageType) => damageType switch
	{
		"blunt" => "Stoß",
		"mixed" => "Schneid und Stoß",
		"ranged" => "Fernkampf",
		_ => "Schneid",
	};

	private static string DescribeStats(ItemDefinition item)
	{
		StringBuilder text = new();

		if (item.Type == "weapon")
		{
			text.AppendLine($"Schaden {item.Damage} ({DescribeDamageType(item.DamageType)})");
			text.AppendLine(item.ScalingStat == "dexterity" ? "Skaliert mit Geschick" : "Skaliert mit Stärke");
		}

		if (item.Defense > 0)
			text.AppendLine($"Schutz {item.Defense}");

		if (item.StaggerResist > 0)
			text.AppendLine($"Stagger-Widerstand {item.StaggerResist}");

		if (item.HealAmount > 0)
			text.AppendLine($"Heilt {item.HealAmount} Leben");

		if (item.Price > 0)
			text.AppendLine($"Wert {item.Price} Silber");

		return text.ToString().TrimEnd();
	}
}
