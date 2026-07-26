using DialogueManagerRuntime;
using Godot;
using Godot.Collections;

// Uebernommen aus addons/dialogue_manager/example_balloon/ExampleBalloon.cs, damit ein Update des
// Addons unsere Anpassungen nicht ueberschreibt. Bewusst nah am Original gehalten - so laesst sich
// bei einem Addon-Update leicht vergleichen, was sich dort geaendert hat.
//
// Das ist die Dialogbox selbst: Sprechername, Text, Antwortknoepfe. Wer das Aussehen aendern will,
// aendert UI/DialogueBalloon.tscn (Theme/Layout) - hier steht nur das Verhalten.
//
// Geborgter Code: nullable-Pruefung aus, sonst meldet der Compiler das Original-Muster an.
#nullable disable

namespace RPG.UI;

public partial class DialogueBalloon : CanvasLayer
{
	[Export] public Resource DialogueResource;
	[Export] public string StartFromTitle = "";
	[Export] public bool AutoStart = false;

	// Weiterklicken / Antwort bestaetigen (Leertaste, Enter) bzw. Tippanimation ueberspringen.
	[Export] public string NextAction = "ui_accept";
	[Export] public string SkipAction = "ui_cancel";

	private Control _balloon;
	private RichTextLabel _characterLabel;
	private RichTextLabel _dialogueLabel;
	private VBoxContainer _responsesMenu;
	private Polygon2D _progress;

	private Array<Variant> _temporaryGameStates = new();
	private bool _isWaitingForInput;
	private bool _willHideBalloon;

	private readonly Timer _mutationCooldown = new();

	private DialogueLine _dialogueLine;

	private DialogueLine CurrentLine
	{
		get => _dialogueLine;
		set
		{
			// Dialog zu Ende -> Box schliessen
			if (value == null)
			{
				if (Owner == null)
					QueueFree();
				else
					Hide();
				return;
			}

			_dialogueLine = value;
			ApplyDialogueLine();
		}
	}

	public override void _Ready()
	{
		_balloon = GetNode<Control>("%Balloon");
		_characterLabel = GetNode<RichTextLabel>("%CharacterLabel");
		_dialogueLabel = GetNode<RichTextLabel>("%DialogueLabel");
		_responsesMenu = GetNode<VBoxContainer>("%ResponsesMenu");
		_progress = GetNode<Polygon2D>("%Progress");

		_balloon.Hide();

		_balloon.GuiInput += OnBalloonGuiInput;

		if (string.IsNullOrEmpty((string)_responsesMenu.Get("next_action")))
			_responsesMenu.Set("next_action", NextAction);

		_responsesMenu.Connect("response_selected", Callable.From((DialogueResponse response) => Next(response.NextId)));

		// Box waehrend einer laufenden Mutation (`do ...`) kurz ausblenden
		_mutationCooldown.Timeout += () =>
		{
			if (_willHideBalloon)
			{
				_willHideBalloon = false;
				_balloon.Hide();
			}
		};
		AddChild(_mutationCooldown);

		DialogueManager.Mutated += OnMutated;

		if (AutoStart)
		{
			if (!IsInstanceValid(DialogueResource))
				throw new System.Exception(DialogueManager.GetErrorMessage(143));

			Start();
		}
	}

	public override void _ExitTree()
	{
		DialogueManager.Mutated -= OnMutated;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Solange die Box offen ist, darf nichts anderes auf Eingaben reagieren.
		GetViewport().SetInputAsHandled();
	}

	public override async void _Notification(int what)
	{
		// Sprache gewechselt -> aktuelle Zeile neu holen
		if (what == NotificationTranslationChanged && IsInstanceValid(_dialogueLabel))
		{
			float visibleRatio = _dialogueLabel.VisibleRatio;
			CurrentLine = await DialogueManager.GetNextDialogueLine(DialogueResource, CurrentLine.Id, _temporaryGameStates);
			if (visibleRatio < 1.0f)
				_dialogueLabel.Call("skip_typing");
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (IsInstanceValid(_dialogueLine))
			_progress.Visible = !(bool)_dialogueLabel.Get("is_typing") && _dialogueLine.Responses.Count == 0 && !_dialogueLine.HasTag("voice");
	}

	// Wird vom Addon aufgerufen (dialogue_manager.gd -> _start_balloon sucht "start" oder "Start").
	public async void Start(Resource dialogueResource = null, string title = "", Array<Variant> extraGameStates = null)
	{
		_temporaryGameStates = new Array<Variant> { this } + (extraGameStates ?? new Array<Variant>());
		_isWaitingForInput = false;

		if (IsInstanceValid(dialogueResource))
			DialogueResource = dialogueResource;

		if (title != "")
			StartFromTitle = title;

		CurrentLine = await DialogueManager.GetNextDialogueLine(DialogueResource, StartFromTitle, _temporaryGameStates);
		Show();
	}

	public async void Next(string nextId)
	{
		CurrentLine = await DialogueManager.GetNextDialogueLine(DialogueResource, nextId, _temporaryGameStates);
	}

	private void OnBalloonGuiInput(InputEvent @event)
	{
		if ((bool)_dialogueLabel.Get("is_typing"))
		{
			bool mouseWasClicked = @event is InputEventMouseButton mouseButton
				&& mouseButton.ButtonIndex == MouseButton.Left && @event.IsPressed();
			bool skipButtonWasPressed = @event.IsActionPressed(SkipAction);

			if (mouseWasClicked || skipButtonWasPressed)
			{
				GetViewport().SetInputAsHandled();
				_dialogueLabel.Call("skip_typing");
				return;
			}
		}

		if (!_isWaitingForInput)
			return;

		if (_dialogueLine.Responses.Count > 0)
			return;

		GetViewport().SetInputAsHandled();

		if (@event is InputEventMouseButton click && click.IsPressed() && click.ButtonIndex == MouseButton.Left)
			Next(_dialogueLine.NextId);
		else if (@event.IsActionPressed(NextAction) && GetViewport().GuiGetFocusOwner() == _balloon)
			Next(_dialogueLine.NextId);
	}

	private async void ApplyDialogueLine()
	{
		_mutationCooldown.Stop();

		_isWaitingForInput = false;
		_balloon.FocusMode = Control.FocusModeEnum.All;
		_balloon.GrabFocus();

		// Sprechername
		_characterLabel.Visible = !string.IsNullOrEmpty(_dialogueLine.Character);
		_characterLabel.Text = Tr(_dialogueLine.Character, "dialogue");

		// Text
		_dialogueLabel.Hide();
		_dialogueLabel.Set("dialogue_line", _dialogueLine);

		// Antwortmoeglichkeiten
		_responsesMenu.Hide();
		_responsesMenu.Set("responses", _dialogueLine.Responses);

		// Text ausschreiben
		_balloon.Show();
		_willHideBalloon = false;
		_dialogueLabel.Show();
		if (!string.IsNullOrEmpty(_dialogueLine.Text))
		{
			_dialogueLabel.Call("type_out");
			await ToSignal(_dialogueLabel, "finished_typing");
		}

		// Auf Eingabe warten
		if (_dialogueLine.Responses.Count > 0)
		{
			_balloon.FocusMode = Control.FocusModeEnum.None;
			_responsesMenu.Show();
		}
		else if (!string.IsNullOrEmpty(_dialogueLine.Time))
		{
			if (!float.TryParse(_dialogueLine.Time, out float time))
				time = _dialogueLine.Text.Length * 0.02f;

			await ToSignal(GetTree().CreateTimer(time), "timeout");
			Next(_dialogueLine.NextId);
		}
		else
		{
			_isWaitingForInput = true;
			_balloon.FocusMode = Control.FocusModeEnum.All;
			_balloon.GrabFocus();
		}
	}

	private void OnMutated(Dictionary mutation)
	{
		if (!(bool)mutation["is_inline"])
		{
			_isWaitingForInput = false;
			_willHideBalloon = true;
			_mutationCooldown.Start(0.1f);
		}
	}
}
