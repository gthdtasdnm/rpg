using Godot;

/// <summary>
/// Live-Regler fuer die Schattenreichweite der Baeume. Als Kind-Knoten in World/world.tscn
/// haengen, dann im Inspector schieben - die Werte gehen sofort ans Licht und an alle
/// Terrain3D-Baum-Assets, im Editor wie im laufenden Spiel.
///
/// WARUM: Terrain3D hat keinen einzelnen Wert "Schattenreichweite". Sie ergibt sich aus dem
/// Zusammenspiel von drei Einstellungen an unterschiedlichen Stellen, und jede Aenderung hiess
/// bisher: Datei bearbeiten, Godot neu laden, hinsehen. Hier sind sie an einem Ort und wirken
/// sofort.
///
/// Die eingestellten Werte werden NICHT gespeichert, solange world.tscn nicht gespeichert wird -
/// gefundene Einstellung also im Inspector ablesen und in die Szene uebernehmen.
/// </summary>
[Tool]
[GlobalClass]
public partial class ShadowTuner : Node
{
	private float _shadowDistance = 160.0f;
	private float _treeLod0Range = 95.0f;
	private bool _imposterCastsShadows = true;
	private bool _shadowsFromImposterOnly = true;

	/// <summary>
	/// Harte Obergrenze: dahinter wirft nichts mehr Schatten (DirectionalLight3D).
	/// Achtung, das ist ein Tauschgeschaeft - die Schattenkarte hat eine feste Groesse, doppelte
	/// Reichweite heisst halb so scharfe Schatten im Nahbereich.
	/// </summary>
	[ExportGroup("Schatten")]
	[Export(PropertyHint.Range, "20,600,5")]
	public float ShadowDistance
	{
		get => _shadowDistance;
		set { _shadowDistance = value; Apply(); }
	}

	/// <summary>
	/// Ab dieser Entfernung wird aus dem echten Baum das Imposter-Kreuz (lod0_range).
	/// Groesser = laenger volle Meshes = mehr Dreiecke.
	/// </summary>
	[Export(PropertyHint.Range, "20,400,5")]
	public float TreeLod0Range
	{
		get => _treeLod0Range;
		set { _treeLod0Range = value; Apply(); }
	}

	/// <summary>
	/// last_shadow_lod: darf das Imposter-Kreuz (LOD1) ueberhaupt Schatten werfen? Aus heisst,
	/// Baumschatten enden bei TreeLod0Range, egal wie gross ShadowDistance ist.
	/// </summary>
	[Export]
	public bool ImposterCastsShadows
	{
		get => _imposterCastsShadows;
		set { _imposterCastsShadows = value; Apply(); }
	}

	/// <summary>
	/// shadow_impostor: das Kreuz wirft den Schatten auch fuer die nahen, echten Baeume.
	/// Damit gibt es nur EINEN Schattenwerfer ueber die ganze Distanz und keinen Umschaltpunkt -
	/// das Mittel gegen die Luecke im Wald. Preis: nah sieht man die Kreuz-Silhouette statt
	/// echter Blattschatten. Aus = jeder LOD wirft seinen eigenen Schatten.
	/// </summary>
	[Export]
	public bool ShadowsFromImposterOnly
	{
		get => _shadowsFromImposterOnly;
		set { _shadowsFromImposterOnly = value; Apply(); }
	}

	/// <summary>Haken setzen = einmal neu anwenden (z.B. nach dem Laden der Szene).</summary>
	[Export]
	public bool ApplyNow
	{
		get => false;
		set { if (value) Apply(true); }
	}

	public override void _Ready()
	{
		Apply();
	}

	private void Apply(bool verbose = false)
	{
		if (!IsInsideTree())
		{
			return;
		}

		Node root = Owner ?? GetTree()?.EditedSceneRoot ?? GetTree()?.CurrentScene;
		if (root == null)
		{
			return;
		}

		if (FindByClass(root, "DirectionalLight3D") is DirectionalLight3D light)
		{
			light.DirectionalShadowMaxDistance = _shadowDistance;
		}

		Node terrain = FindByClass(root, "Terrain3D");
		if (terrain == null)
		{
			return;
		}

		if (terrain.Call("get_assets").AsGodotObject() is not GodotObject assets)
		{
			return;
		}

		// Wenn nur das Kreuz Schatten wirft, MUSS es sie auch werfen duerfen - sonst haetten die
		// Baeume gar keinen Schatten mehr.
		int lastShadowLod = (_imposterCastsShadows || _shadowsFromImposterOnly) ? 1 : 0;

		int count = assets.Call("get_mesh_count").AsInt32();
		int touched = 0;

		for (int i = 0; i < count; i++)
		{
			if (assets.Call("get_mesh_asset", i).AsGodotObject() is not GodotObject asset)
			{
				continue;
			}

			// Steine, Baumstuempfe und die Gras-Karte haben kein LOD1 - fuer die waere ein
			// Imposter-Schatten sinnlos, und last_shadow_lod > last_lod ist ungueltig.
			if (asset.Get("last_lod").AsInt32() < 1)
			{
				continue;
			}

			asset.Set("last_shadow_lod", lastShadowLod);
			asset.Set("shadow_impostor", _shadowsFromImposterOnly ? 1 : 0);
			asset.Set("lod0_range", _treeLod0Range);
			touched++;
		}

		if (verbose)
		{
			GD.Print($"ShadowTuner: {touched} Baum-Assets, Distanz {_shadowDistance} m, " +
				$"LOD0 bis {_treeLod0Range} m, last_shadow_lod {lastShadowLod}, " +
				$"shadow_impostor {(_shadowsFromImposterOnly ? 1 : 0)}");
		}
	}

	private static Node FindByClass(Node node, string className)
	{
		if (node.IsClass(className))
		{
			return node;
		}

		foreach (Node child in node.GetChildren())
		{
			Node found = FindByClass(child, className);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}
}
