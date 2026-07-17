using Godot;
using RPG.Characters;
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
	private enum PanelKind { None, Pause, Inventory, QuestLog }

	private ProgressBar _healthBar = null!;
	private Label _healthValueLabel = null!;
	private Label _interactionPrompt = null!;
	private Control _dialoguePanel = null!;
	private Label _dialogueName = null!;
	private Label _dialogueText = null!;
	private VBoxContainer _choicesBox = null!;

	private Control _pausePanel = null!;
	private Label _pauseStatusLabel = null!;
	private Control _inventoryPanel = null!;
	private VBoxContainer _inventoryItemsBox = null!;
	private Control _questLogPanel = null!;
	private VBoxContainer _questLogEntriesBox = null!;

	private Inventory? _inventory;
	private PanelKind _openPanel = PanelKind.None;

	public override void _Ready()
	{
		_healthBar = GetNode<ProgressBar>("HealthBar");
		_healthValueLabel = GetNode<Label>("HealthBar/HealthValueLabel");
		_interactionPrompt = GetNode<Label>("InteractionPrompt");
		_dialoguePanel = GetNode<Control>("DialoguePanel");
		_dialogueName = GetNode<Label>("DialoguePanel/Margin/Box/NameLabel");
		_dialogueText = GetNode<Label>("DialoguePanel/Margin/Box/TextLabel");
		_choicesBox = GetNode<VBoxContainer>("DialoguePanel/Margin/Box/ChoicesBox");

		_pausePanel = GetNode<Control>("PausePanel");
		_pauseStatusLabel = GetNode<Label>("PausePanel/Margin/Box/StatusLabel");
		_inventoryPanel = GetNode<Control>("InventoryPanel");
		_inventoryItemsBox = GetNode<VBoxContainer>("InventoryPanel/Margin/Box/Scroll/ItemsBox");
		_questLogPanel = GetNode<Control>("QuestLogPanel");
		_questLogEntriesBox = GetNode<VBoxContainer>("QuestLogPanel/Margin/Box/Scroll/EntriesBox");

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
		}

		DialogueRunner.Instance.LineChanged += OnLineChanged;
		DialogueRunner.Instance.DialogueEnded += OnDialogueEnded;

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
			if (!DialogueRunner.Instance.IsActive)
				OnEscape();
			return;
		}

		if (DialogueRunner.Instance.IsActive)
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

		if (kind == PanelKind.Pause)
			_pauseStatusLabel.Text = "";
		else if (kind == PanelKind.Inventory)
			RefreshInventory();
		else if (kind == PanelKind.QuestLog)
			RefreshQuestLog();
	}

	private void ClosePanel()
	{
		_openPanel = PanelKind.None;
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_pausePanel.Visible = false;
		_inventoryPanel.Visible = false;
		_questLogPanel.Visible = false;
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

	private void OnLineChanged(string speaker, string text, string[] choices)
	{
		_interactionPrompt.Visible = false;
		_dialoguePanel.Visible = true;
		_dialogueName.Text = speaker;
		_dialogueText.Text = text;

		foreach (Node child in _choicesBox.GetChildren())
			child.QueueFree();

		for (int i = 0; i < choices.Length; i++)
		{
			int index = i;
			Button button = new() { Text = choices[i] };
			button.Pressed += () => DialogueRunner.Instance.Choose(index);
			_choicesBox.AddChild(button);
		}
	}

	private void OnDialogueEnded()
	{
		_dialoguePanel.Visible = false;
	}

	private void OnHealthChanged(int currentHealth, int maxHealth)
	{
		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = currentHealth;
		_healthValueLabel.Text = $"{currentHealth}/{maxHealth}";
	}

	private void OnInventoryChanged()
	{
		if (_openPanel == PanelKind.Inventory)
			RefreshInventory();
	}

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

				string name = GameData.Instance.GetItem(entry.Key)?.Name ?? entry.Key;
				_inventoryItemsBox.AddChild(new Label { Text = $"{name}  ×{entry.Value}" });
			}
		}

		if (_inventoryItemsBox.GetChildCount() == 0)
			_inventoryItemsBox.AddChild(new Label { Text = "(leer)" });
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
