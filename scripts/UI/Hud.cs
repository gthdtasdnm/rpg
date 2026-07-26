using Godot;
using RPG.Characters;
using RPG.Combat;
using RPG.Data;
using RPG.Dialogue;
using RPG.Interaction;
using RPG.Items;
using RPG.Quests;
using RPG.World;
using System.Collections.Generic;

namespace RPG.UI;

public partial class Hud : CanvasLayer
{
	private enum PanelKind { None, Pause, Inventory, QuestLog, Shop }

	private ProgressBar _healthBar = null!;
	private Label _healthValueLabel = null!;
	private Label _silverLabel = null!;
	private Label _interactionPrompt = null!;

	private Control _pausePanel = null!;
	private Label _pauseStatusLabel = null!;
	private Control _inventoryPanel = null!;
	private VBoxContainer _inventoryItemsBox = null!;
	private Control _questLogPanel = null!;
	private VBoxContainer _questLogEntriesBox = null!;
	private Control _shopPanel = null!;
	private Label _shopTitleLabel = null!;
	private Label _shopSilverLabel = null!;
	private VBoxContainer _shopBuyBox = null!;
	private VBoxContainer _shopSellBox = null!;

	private Inventory? _inventory;
	private Equipment? _equipment;
	private PanelKind _openPanel = PanelKind.None;
	private string? _shopCharacterId;

	public override void _Ready()
	{
		_healthBar = GetNode<ProgressBar>("HealthBar");
		_healthValueLabel = GetNode<Label>("HealthBar/HealthValueLabel");
		// Die Knoten in Hud.tscn heissen noch "GoldLabel" - die Waehrung heisst inzwischen Silber,
		// die Szene wurde dafuer aber nicht umbenannt (siehe Inventory.cs).
		_silverLabel = GetNode<Label>("GoldLabel");
		_interactionPrompt = GetNode<Label>("InteractionPrompt");

		_pausePanel = GetNode<Control>("PausePanel");
		_pauseStatusLabel = GetNode<Label>("PausePanel/Margin/Box/StatusLabel");
		_inventoryPanel = GetNode<Control>("InventoryPanel");
		_inventoryItemsBox = GetNode<VBoxContainer>("InventoryPanel/Margin/Box/Scroll/ItemsBox");
		_questLogPanel = GetNode<Control>("QuestLogPanel");
		_questLogEntriesBox = GetNode<VBoxContainer>("QuestLogPanel/Margin/Box/Scroll/EntriesBox");
		_shopPanel = GetNode<Control>("ShopPanel");
		_shopTitleLabel = GetNode<Label>("ShopPanel/Margin/Box/TitleLabel");
		_shopSilverLabel = GetNode<Label>("ShopPanel/Margin/Box/GoldLabel");
		_shopBuyBox = GetNode<VBoxContainer>("ShopPanel/Margin/Box/Columns/BuyColumn/BuyScroll/BuyBox");
		_shopSellBox = GetNode<VBoxContainer>("ShopPanel/Margin/Box/Columns/SellColumn/SellScroll/SellBox");

		_interactionPrompt.Visible = false;

		Node? player = GetTree().GetFirstNodeInGroup("player");
		if (player != null)
		{
			Interactor interactor = player.GetNode<Interactor>("CameraPivot/Camera3D/Interactor");
			interactor.TargetChanged += OnTargetChanged;
			interactor.TargetLost += OnTargetLost;

			CharacterStats stats = player.GetNode<CharacterStats>("Stats");
			stats.HealthChanged += OnHealthChanged;
			OnHealthChanged(stats.CurrentHealth, stats.Definition.MaxHealth);

			_inventory = player.GetNode<Inventory>("Inventory");
			_inventory.InventoryChanged += OnInventoryChanged;
			_inventory.SilverChanged += OnSilverChanged;
			OnSilverChanged(_inventory.Silver);

			_equipment = player.GetNode<Equipment>("Equipment");
			_equipment.WeaponChanged += (_) => RefreshInventoryIfOpen();
			_equipment.ShieldChanged += (_) => RefreshInventoryIfOpen();
			_equipment.ArmorChanged += (_) => RefreshInventoryIfOpen();
		}

		// Die Dialogbox selbst gehoert zum Dialogue-Manager-Addon (UI/DialogueBalloon.tscn), nicht
		// mehr zum HUD. Das HUD muss nur wissen, wann geredet wird - und wann ein Dialog das
		// Haendlerfenster oeffnen will.
		DialogueBridge.Instance.DialogueStarted += OnDialogueStarted;
		DialogueBridge.Instance.ShopRequested += OnShopRequested;

		QuestManager.Instance.QuestStarted += (_, _) => RefreshQuestLogIfOpen();
		QuestManager.Instance.QuestProgressed += (_, _, _) => RefreshQuestLogIfOpen();
		QuestManager.Instance.QuestCompleted += (_, _) => RefreshQuestLogIfOpen();

		GetNode<Button>("PausePanel/Margin/Box/ResumeButton").Pressed += ClosePanel;
		GetNode<Button>("PausePanel/Margin/Box/SaveButton").Pressed += OnSavePressed;
		GetNode<Button>("PausePanel/Margin/Box/LoadButton").Pressed += OnLoadPressed;
		GetNode<Button>("PausePanel/Margin/Box/QuitButton").Pressed += () => GetTree().Quit();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape)
		{
			if (!DialogueBridge.Instance.IsActive)
				OnEscape();
			return;
		}

		if (DialogueBridge.Instance.IsActive)
			return;

		if (@event.IsActionPressed("toggle_inventory"))
			OnToggleInventory();
		else if (@event.IsActionPressed("toggle_questlog"))
			OnToggleQuestLog();
	}

	private void OnEscape()
	{
		if (_openPanel != PanelKind.None)
			ClosePanel();
		else
			OpenPanel(PanelKind.Pause);
	}

	private void OnToggleInventory()
	{
		if (_openPanel == PanelKind.Inventory)
			ClosePanel();
		else if (_openPanel == PanelKind.None)
			OpenPanel(PanelKind.Inventory);
	}

	private void OnToggleQuestLog()
	{
		if (_openPanel == PanelKind.QuestLog)
			ClosePanel();
		else if (_openPanel == PanelKind.None)
			OpenPanel(PanelKind.QuestLog);
	}

	private void OpenPanel(PanelKind kind)
	{
		_openPanel = kind;
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		_pausePanel.Visible = kind == PanelKind.Pause;
		_inventoryPanel.Visible = kind == PanelKind.Inventory;
		_questLogPanel.Visible = kind == PanelKind.QuestLog;
		_shopPanel.Visible = kind == PanelKind.Shop;

		if (kind == PanelKind.Pause)
			_pauseStatusLabel.Text = "";
		else if (kind == PanelKind.Inventory)
			RefreshInventory();
		else if (kind == PanelKind.QuestLog)
			RefreshQuestLog();
		else if (kind == PanelKind.Shop)
			RefreshShop();
	}

	private void ClosePanel()
	{
		_openPanel = PanelKind.None;
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_pausePanel.Visible = false;
		_inventoryPanel.Visible = false;
		_questLogPanel.Visible = false;
		_shopPanel.Visible = false;
		_shopCharacterId = null;
	}

	// Wird aus einem Dialog ueber `do Dialog.OpenShop("npc_id")` ausgeloest.
	// Der Dialog laeuft zu diesem Zeitpunkt noch - das Panel legt sich einfach darueber.
	private void OnShopRequested(string characterId)
	{
		_shopCharacterId = characterId;
		OpenPanel(PanelKind.Shop);
	}

	private void OnSavePressed()
	{
		bool ok = SaveSystem.Instance.Save();
		_pauseStatusLabel.Text = ok ? "Gespeichert." : "Speichern fehlgeschlagen.";
	}

	private void OnLoadPressed()
	{
		bool ok = SaveSystem.Instance.Load();
		_pauseStatusLabel.Text = ok ? "Geladen." : "Kein Spielstand gefunden.";
	}

	private void OnTargetChanged(string prompt)
	{
		_interactionPrompt.Text = prompt;
		_interactionPrompt.Visible = true;
	}

	private void OnTargetLost()
	{
		_interactionPrompt.Visible = false;
	}

	private void OnDialogueStarted()
	{
		_interactionPrompt.Visible = false;
	}

	private void OnHealthChanged(int currentHealth, int maxHealth)
	{
		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = currentHealth;
		_healthValueLabel.Text = $"{currentHealth}/{maxHealth}";
	}

	private void OnInventoryChanged() => RefreshInventoryIfOpen();

	private void RefreshInventoryIfOpen()
	{
		if (_openPanel == PanelKind.Inventory)
			RefreshInventory();
	}

	// Ausruestbare Typen (siehe Equipment.cs) bekommen einen "Ausrüsten"-Knopf; das aktuell
	// ausgeruestete Item einer Kategorie wird stattdessen als "(ausgerüstet)" markiert.
	private void RefreshInventory()
	{
		foreach (Node child in _inventoryItemsBox.GetChildren())
			child.QueueFree();

		if (_inventory != null)
		{
			foreach (KeyValuePair<string, int> entry in _inventory.GetAllItems())
			{
				if (entry.Value <= 0)
					continue;

				ItemDefinition? item = GameData.Instance.GetItem(entry.Key);
				string name = item?.Name ?? entry.Key;

				HBoxContainer row = new();
				row.AddChild(new Label { Text = $"{name}  ×{entry.Value}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

				bool isEquippable = item != null && (item.Type == "weapon" || item.Type == "shield" || item.Type == "armor");
				bool isEquipped = entry.Key == _equipment?.EquippedWeaponId
					|| entry.Key == _equipment?.EquippedShieldId
					|| entry.Key == _equipment?.EquippedArmorId;

				if (isEquipped)
				{
					row.AddChild(new Label { Text = "(ausgerüstet)" });
				}
				else if (isEquippable)
				{
					string itemId = entry.Key;
					Button equipButton = new() { Text = "Ausrüsten" };
					equipButton.Pressed += () => _equipment?.Equip(itemId);
					row.AddChild(equipButton);
				}

				_inventoryItemsBox.AddChild(row);
			}
		}

		if (_inventoryItemsBox.GetChildCount() == 0)
			_inventoryItemsBox.AddChild(new Label { Text = "(leer)" });
	}

	private void OnSilverChanged(int totalSilver)
	{
		_silverLabel.Text = $"Silber: {totalSilver}";
		if (_openPanel == PanelKind.Shop)
			_shopSilverLabel.Text = $"Silber: {totalSilver}";
	}

	// Kaufen-Spalte = Haendler-Sortiment (CharacterDefinition.ShopItemIds), Verkaufen-Spalte =
	// eigenes Inventar (nur Items mit Price > 0, keine Questgegenstaende). "skill"-Items (siehe
	// ItemDefinition-Kommentar) setzen beim Kauf ein Lern-Flag statt einen Inventareintrag zu
	// erzeugen und tauchen deshalb nie in der Verkaufen-Spalte auf.
	private void RefreshShop()
	{
		CharacterDefinition? merchant = _shopCharacterId != null ? GameData.Instance.GetCharacter(_shopCharacterId) : null;
		_shopTitleLabel.Text = merchant?.Name ?? "Händler";
		_shopSilverLabel.Text = $"Silber: {_inventory?.Silver ?? 0}";

		foreach (Node child in _shopBuyBox.GetChildren())
			child.QueueFree();

		if (merchant != null)
		{
			foreach (string itemId in merchant.ShopItemIds)
			{
				ItemDefinition? item = GameData.Instance.GetItem(itemId);
				if (item == null)
					continue;

				bool alreadyLearned = item.Type == "skill" && GameFlags.Instance.HasFlag($"learned_{itemId}");

				HBoxContainer row = new();
				row.AddChild(new Label { Text = $"{item.Name} — {item.Price} Silber", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

				if (alreadyLearned)
				{
					row.AddChild(new Label { Text = "(gelernt)" });
				}
				else
				{
					Button buyButton = new() { Text = "Kaufen" };
					buyButton.Pressed += () => OnBuyPressed(itemId, item);
					row.AddChild(buyButton);
				}

				_shopBuyBox.AddChild(row);
			}
		}

		if (_shopBuyBox.GetChildCount() == 0)
			_shopBuyBox.AddChild(new Label { Text = "(nichts im Angebot)" });

		foreach (Node child in _shopSellBox.GetChildren())
			child.QueueFree();

		if (_inventory != null)
		{
			foreach (KeyValuePair<string, int> entry in _inventory.GetAllItems())
			{
				if (entry.Value <= 0)
					continue;

				ItemDefinition? item = GameData.Instance.GetItem(entry.Key);
				if (item == null || item.Price <= 0 || item.Type == "quest")
					continue;

				int sellPrice = Mathf.Max(1, item.Price / 2);
				string itemId = entry.Key;

				HBoxContainer row = new();
				row.AddChild(new Label { Text = $"{item.Name} ×{entry.Value} — {sellPrice} Silber", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

				Button sellButton = new() { Text = "Verkaufen" };
				sellButton.Pressed += () => OnSellPressed(itemId, sellPrice);
				row.AddChild(sellButton);

				_shopSellBox.AddChild(row);
			}
		}

		if (_shopSellBox.GetChildCount() == 0)
			_shopSellBox.AddChild(new Label { Text = "(nichts zu verkaufen)" });
	}

	private void OnBuyPressed(string itemId, ItemDefinition item)
	{
		if (_inventory == null || !_inventory.SpendSilver(item.Price))
			return;

		if (item.Type == "skill")
			GameFlags.Instance.SetFlag($"learned_{itemId}");
		else
			_inventory.AddItem(itemId);

		RefreshShop();
	}

	private void OnSellPressed(string itemId, int sellPrice)
	{
		if (_inventory == null || !_inventory.RemoveItem(itemId, 1))
			return;

		_inventory.AddSilver(sellPrice);
		RefreshShop();
	}

	private void RefreshQuestLogIfOpen()
	{
		if (_openPanel == PanelKind.QuestLog)
			RefreshQuestLog();
	}

	private void RefreshQuestLog()
	{
		foreach (Node child in _questLogEntriesBox.GetChildren())
			child.QueueFree();

		List<QuestManager.QuestLogEntry> entries = QuestManager.Instance.GetQuestLogEntries();
		if (entries.Count == 0)
		{
			_questLogEntriesBox.AddChild(new Label { Text = "(noch keine Quests)" });
			return;
		}

		foreach (QuestManager.QuestLogEntry entry in entries)
		{
			Label title = new() { Text = entry.Title };
			title.AddThemeFontSizeOverride("font_size", 18);
			_questLogEntriesBox.AddChild(title);

			if (!string.IsNullOrEmpty(entry.Status))
				_questLogEntriesBox.AddChild(new Label { Text = entry.Status });
		}
	}
}
