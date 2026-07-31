using Godot;
using RPG.Data;

namespace RPG.UI;

// Ein Feld im Inventar-Raster: Icon, Stückzahl, Ausgerüstet-Marke.
//
// Bewusst komplett in Code gebaut statt als eigene Szene - die Felder werden zur Laufzeit erzeugt
// (ein Feld je besessenem Item) und sollen im Raster und in den Ausrüstungsslots gleich aussehen.
//
// Ein Klick wählt aus (Pressed), ein Doppelklick löst direkt die Standardaktion aus (Activated) -
// dasselbe, was in der Detailspalte auf dem oberen Knopf steht.
public partial class ItemSlotButton : Button
{
	[Signal] public delegate void ActivatedEventHandler();

	public const int SlotSize = 74;

	private static readonly Color FrameColor = new(0.42f, 0.36f, 0.23f, 0.85f);
	private static readonly Color FrameColorHover = new(0.72f, 0.61f, 0.36f, 1f);
	private static readonly Color FrameColorSelected = new(0.88f, 0.76f, 0.45f, 1f);

	public string ItemId { get; private set; } = "";

	private TextureRect _icon = null!;
	private Label _fallbackLabel = null!;
	private Label _countLabel = null!;
	private Label _equippedMark = null!;

	public override void _Ready()
	{
		ToggleMode = true;
		CustomMinimumSize = new Vector2(SlotSize, SlotSize);
		ClipContents = true;
		FocusMode = FocusModeEnum.All;

		AddThemeStyleboxOverride("normal", BuildStyle(new Color(0.13f, 0.13f, 0.15f, 0.9f), FrameColor, 1));
		AddThemeStyleboxOverride("hover", BuildStyle(new Color(0.19f, 0.18f, 0.16f, 0.95f), FrameColorHover, 2));
		AddThemeStyleboxOverride("pressed", BuildStyle(new Color(0.23f, 0.20f, 0.13f, 0.98f), FrameColorSelected, 2));
		AddThemeStyleboxOverride("focus", BuildStyle(new Color(0f, 0f, 0f, 0f), FrameColorSelected, 2));
		AddThemeStyleboxOverride("disabled", BuildStyle(new Color(0.10f, 0.10f, 0.11f, 0.7f), FrameColor, 1));

		_icon = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_icon.OffsetLeft = 5;
		_icon.OffsetTop = 5;
		_icon.OffsetRight = -5;
		_icon.OffsetBottom = -5;
		AddChild(_icon);

		// Ersatz für Items ohne Modell: ein Namenskürzel, damit das Feld nie leer wirkt.
		_fallbackLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		_fallbackLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_fallbackLabel.AddThemeFontSizeOverride("font_size", 22);
		_fallbackLabel.AddThemeColorOverride("font_color", new Color(0.62f, 0.57f, 0.44f));
		AddChild(_fallbackLabel);

		_countLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		_countLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_countLabel.OffsetRight = -5;
		_countLabel.OffsetBottom = -3;
		_countLabel.AddThemeFontSizeOverride("font_size", 14);
		_countLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.82f));
		_countLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
		_countLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		AddChild(_countLabel);

		_equippedMark = new Label
		{
			Text = "◆",
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		_equippedMark.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
		_equippedMark.OffsetLeft = 4;
		_equippedMark.OffsetTop = 1;
		_equippedMark.AddThemeFontSizeOverride("font_size", 13);
		_equippedMark.AddThemeColorOverride("font_color", new Color(0.95f, 0.80f, 0.40f));
		AddChild(_equippedMark);
	}

	// Ein leeres Item (null) ergibt einen leeren Ausrüstungsslot - deshalb ist das kein Fehlerfall.
	public void SetItem(ItemDefinition? item, int count, bool isEquipped)
	{
		ItemId = item?.Id ?? "";
		TooltipText = item?.Name ?? "";

		Texture2D? icon = ItemIcons.Instance?.GetIcon(item);
		_icon.Texture = icon;
		_fallbackLabel.Visible = icon == null && item != null;
		_fallbackLabel.Text = item != null ? Abbreviate(item.Name) : "";

		_countLabel.Visible = count > 1;
		_countLabel.Text = count > 1 ? $"×{count}" : "";

		_equippedMark.Visible = isEquipped;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { DoubleClick: true, ButtonIndex: MouseButton.Left })
			EmitSignal(SignalName.Activated);

		base._GuiInput(@event);
	}

	private static string Abbreviate(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return "?";

		string[] words = name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
		if (words.Length >= 2)
			return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";

		return words[0].Length >= 2 ? words[0][..2] : words[0];
	}

	private static StyleBoxFlat BuildStyle(Color background, Color border, int borderWidth) => new()
	{
		BgColor = background,
		BorderColor = border,
		BorderWidthLeft = borderWidth,
		BorderWidthTop = borderWidth,
		BorderWidthRight = borderWidth,
		BorderWidthBottom = borderWidth,
		CornerRadiusTopLeft = 4,
		CornerRadiusTopRight = 4,
		CornerRadiusBottomRight = 4,
		CornerRadiusBottomLeft = 4,
	};
}
