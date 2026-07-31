using Godot;
using RPG.Data;
using RPG.Items;
using System.Collections.Generic;

namespace RPG.UI;

// Autoload: macht aus dem 3D-Modell eines Items sein Inventar-Icon.
//
// Statt für jedes Item ein Bild zu malen, wird das Modell aus `ItemDefinition.Model` in einen
// eigenen kleinen SubViewport gestellt, automatisch gerahmt und **einmal** gerendert
// (UpdateMode.Once). Die Textur bleibt danach stehen und kostet nichts mehr - neues Item heisst
// also weiterhin nur: neue JSON-Datei, kein Bild, kein Code.
//
// Gedreht wird immer das Objekt (iconYaw/iconPitch/iconRoll in der JSON), nie die Kamera; die
// steht frontal und orthogonal und passt ihren Ausschnitt an die tatsaechliche Ausdehnung des
// gedrehten Modells an. Dadurch fuellt jedes Item sein Feld gleich gut aus, egal wie gross es in
// der Welt ist.
//
// Ist `ItemDefinition.Icon` gesetzt, gewinnt dieses fertige Bild und es wird nichts gerendert.
public partial class ItemIcons : Node
{
	public static ItemIcons Instance { get; private set; } = null!;

	// Kantenlaenge der gerenderten Icons. Die Inventar-Felder sind kleiner - lieber etwas zu gross
	// rendern und herunterskalieren als umgekehrt.
	private const int IconSize = 128;

	// Luft zwischen Modell und Feldrand, damit nichts an der Kante klebt.
	private const float FramePadding = 1.12f;

	// Ab diesem Verhaeltnis von laengster zu naechstlaengster Ausdehnung gilt ein Modell als
	// laenglich und wird automatisch ausgerichtet (siehe BuildAutoOrientation).
	//
	// Der Wert ist an den vorhandenen Modellen gemessen und liegt bewusst in der Luecke dazwischen:
	// die laenglichste aufrecht gemeinte Sache ist das Segenswasser mit 2,05 - die kompakteste
	// Waffe die Streitaxt mit 2,76. Trank, Ruestung und Brief bleiben dadurch stehen, jede Waffe
	// wird gelegt. Notausgang fuer Einzelfaelle ist `iconAutoOrient: false` in der JSON.
	private const float ElongatedRatio = 2.4f;

	// Wie weit die Laengsachse eines laenglichen Items aus der Senkrechten gekippt wird.
	private const float DiagonalDegrees = 40f;

	// Null steht fuer "hat kein Icon" und wird bewusst mitgecacht, damit ein fehlendes Modell
	// nicht bei jedem Oeffnen des Inventars erneut geladen wird.
	private readonly Dictionary<string, Texture2D?> _cache = new();

	public override void _Ready()
	{
		Instance = this;

		// Das Inventar pausiert den Baum (siehe Hud.cs). Rendern ist davon zwar nicht betroffen,
		// aber der Knoten soll auch sonst nie stillstehen.
		ProcessMode = ProcessModeEnum.Always;

		// Alles vorab rendern, damit beim ersten Oeffnen des Inventars keine leeren Felder
		// aufblitzen. Verzoegert, weil GameData seine JSONs erst in seinem eigenen _Ready liest.
		CallDeferred(nameof(PrewarmAll));
	}

	private void PrewarmAll()
	{
		if (GameData.Instance == null)
			return;

		foreach (ItemDefinition item in GameData.Instance.GetAllItems())
			GetIcon(item);
	}

	public Texture2D? GetIcon(ItemDefinition? item)
	{
		if (item == null)
			return null;

		if (_cache.TryGetValue(item.Id, out Texture2D? cached))
			return cached;

		Texture2D? icon = string.IsNullOrEmpty(item.Icon)
			? RenderModel(item)
			: ResourceLoader.Load<Texture2D>(item.Icon);

		_cache[item.Id] = icon;
		return icon;
	}

	public Texture2D? GetIcon(string itemId) => GetIcon(GameData.Instance?.GetItem(itemId));

	private Texture2D? RenderModel(ItemDefinition item)
	{
		Node3D? model = ItemModel.Instantiate(item);
		if (model == null)
			return null;

		SubViewport viewport = new()
		{
			Size = new Vector2I(IconSize, IconSize),
			TransparentBg = true,
			OwnWorld3D = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
			Msaa3D = Viewport.Msaa.Msaa4X,
		};
		AddChild(viewport);

		viewport.AddChild(new WorldEnvironment { Environment = BuildEnvironment() });

		// Ein Streiflicht von vorne links oben - genug Kontrast, dass man die Form erkennt, ohne
		// dass die abgewandte Seite absaeuft (dafuer ist das Ambient da).
		DirectionalLight3D keyLight = new()
		{
			LightEnergy = 1.7f,
			RotationDegrees = new Vector3(-40f, 150f, 0f),
		};
		viewport.AddChild(keyLight);

		// Erst ungedreht einhaengen: die automatische Ausrichtung braucht die Ausdehnung des
		// Modells in seinen eigenen Achsen.
		Node3D pivot = new();
		viewport.AddChild(pivot);
		pivot.AddChild(model);

		Basis orientation = item.IconAutoOrient
			? BuildAutoOrientation(ItemModel.ComputeBounds(pivot, Transform3D.Identity))
			: Basis.Identity;

		Basis adjustment = Basis.FromEuler(new Vector3(
			Mathf.DegToRad(item.IconPitch),
			Mathf.DegToRad(item.IconYaw),
			Mathf.DegToRad(item.IconRoll)));

		pivot.Basis = adjustment * orientation;

		Camera3D camera = new() { Projection = Camera3D.ProjectionType.Orthogonal, Near = 0.01f };
		viewport.AddChild(camera);
		FrameContents(camera, pivot, item.IconZoom);

		return viewport.GetTexture();
	}

	// Dreht laengliche Modelle so, dass ihre Laengsachse diagonal im Feld liegt und ihre duennste
	// Achse zur Kamera zeigt - man sieht dadurch immer die breite Seite und nie die Schneide.
	//
	// Noetig, weil die Modellpakete nicht einheitlich sind: das Schwert aus dem einen Paket liegt
	// auf einer anderen Achse als die Keule aus dem naechsten. Kompakte Sachen (Trank, Buch,
	// Schild) bleiben unangetastet, die sehen aufrecht besser aus.
	private static Basis BuildAutoOrientation(Aabb bounds)
	{
		Vector3 size = bounds.Size;
		int longest = LargestAxis(size);
		int shortest = SmallestAxis(size);
		if (longest == shortest)
			return Basis.Identity;

		int middle = 3 - longest - shortest;
		if (size[longest] <= ElongatedRatio * Mathf.Max(size[middle], size[shortest]))
			return Basis.Identity;

		// Quell- und Zielbasis, beide rechtshaendig aufgebaut (Laengsachse, Querachse, Dickachse).
		Vector3 sourceLong = AxisVector(longest);
		Vector3 sourceThin = AxisVector(shortest);
		Basis source = new(sourceLong, sourceThin.Cross(sourceLong), sourceThin);

		float tilt = Mathf.DegToRad(DiagonalDegrees);
		Vector3 targetLong = new(Mathf.Sin(tilt), Mathf.Cos(tilt), 0f);
		Vector3 targetThin = Vector3.Back;
		Basis target = new(targetLong, targetThin.Cross(targetLong), targetThin);

		return target * source.Transposed();
	}

	private static Vector3 AxisVector(int axis) => axis switch
	{
		0 => Vector3.Right,
		1 => Vector3.Up,
		_ => Vector3.Back,
	};

	private static int LargestAxis(Vector3 size)
	{
		if (size.X >= size.Y && size.X >= size.Z)
			return 0;

		return size.Y >= size.Z ? 1 : 2;
	}

	private static int SmallestAxis(Vector3 size)
	{
		if (size.X <= size.Y && size.X <= size.Z)
			return 0;

		return size.Y <= size.Z ? 1 : 2;
	}

	// Setzt die orthogonale Kamera so, dass der Inhalt formatfuellend im Bild liegt: erst die
	// tatsaechliche Ausdehnung messen, dann davor stellen, dann den Ausschnitt exakt darauf
	// zuschneiden.
	private static void FrameContents(Camera3D camera, Node3D contents, float zoom)
	{
		// Weltkoordinaten des Icon-Viewports, also inklusive der Drehung am Pivot.
		Aabb bounds = ItemModel.ComputeBounds(contents, Transform3D.Identity);
		if (bounds.Size.LengthSquared() <= 0f)
			bounds = new Aabb(Vector3.Zero, Vector3.One * 0.1f);

		Vector3 center = bounds.GetCenter();
		float distance = bounds.Size.Length() + 1f;

		camera.GlobalPosition = center + Vector3.Back * distance;
		camera.LookAt(center, Vector3.Up);

		// Die halbe Ausdehnung in Kamerakoordinaten ist genau das, was ins Bild passen muss.
		Transform3D toCamera = camera.GlobalTransform.AffineInverse();
		float halfExtent = 0f;
		for (int i = 0; i < 8; i++)
		{
			Vector3 corner = toCamera * bounds.GetEndpoint(i);
			halfExtent = Mathf.Max(halfExtent, Mathf.Max(Mathf.Abs(corner.X), Mathf.Abs(corner.Y)));
		}

		camera.Size = Mathf.Max(0.01f, halfExtent * 2f * FramePadding / Mathf.Max(0.05f, zoom));
		camera.Far = distance * 2f + bounds.Size.Length();
	}

	// Hintergrund mit Alpha 0, damit das Icon vor dem Inventarfeld freigestellt steht.
	private static Godot.Environment BuildEnvironment() => new()
	{
		BackgroundMode = Godot.Environment.BGMode.Color,
		BackgroundColor = new Color(0f, 0f, 0f, 0f),
		AmbientLightSource = Godot.Environment.AmbientSource.Color,
		AmbientLightColor = new Color(0.55f, 0.57f, 0.66f),
		AmbientLightEnergy = 1.2f,
		TonemapMode = Godot.Environment.ToneMapper.Filmic,
	};
}
