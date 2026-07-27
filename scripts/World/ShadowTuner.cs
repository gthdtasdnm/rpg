using Godot;

/// <summary>
/// EIN Regler: wie weit Schatten reichen. Sonst nichts.
///
/// Alles andere ist fest in World/world.tscn eingestellt und wird hier absichtlich NICHT mehr
/// angeboten - die frueheren vier Regler haben sich gegenseitig verschoben und waren dadurch
/// unbenutzbar. Der feste Aufbau lautet:
///
///   0 - 80 m    echter Baum (LOD0), wirft seinen echten Schatten
///   80 - 600 m  Imposter-Kreuz (LOD1), wirft seinen Imposter-Schatten
///
/// In den Baum-Assets: last_shadow_lod = 1 (beide LODs duerfen werfen), shadow_impostor = 0
/// (jeder LOD wirft seinen eigenen Schatten), lod0_range = 80, lod1_range = 600,
/// fade_margin = 0 (kein Ueberblenden - sonst sieht man Baum UND Kreuz gleichzeitig).
/// Wer daran drehen will, macht das dort und nicht hier.
/// </summary>
[Tool]
[GlobalClass]
public partial class ShadowTuner : Node
{
	private float _shadowDistance = 200.0f;

	/// <summary>
	/// Bis hierhin wirft alles Schatten, was gerade da ist - egal ob echter Baum oder Imposter.
	/// Groesser heisst weiter, aber unschaerfer: die Schattenkarte hat eine feste Aufloesung und
	/// muss bei doppelter Reichweite die vierfache Flaeche abdecken.
	/// </summary>
	[Export(PropertyHint.Range, "20,1000,10")]
	public float ShadowDistance
	{
		get => _shadowDistance;
		set { _shadowDistance = value; Apply(); }
	}

	public override void _Ready()
	{
		Apply();
	}

	private void Apply()
	{
		if (!IsInsideTree())
		{
			return;
		}

		Node? root = Owner ?? GetTree()?.EditedSceneRoot ?? GetTree()?.CurrentScene;
		if (root == null)
		{
			return;
		}

		if (FindByClass(root, "DirectionalLight3D") is DirectionalLight3D light)
		{
			light.DirectionalShadowMaxDistance = _shadowDistance;
		}
	}

	private static Node? FindByClass(Node node, string className)
	{
		if (node.IsClass(className))
		{
			return node;
		}

		foreach (Node child in node.GetChildren())
		{
			Node? found = FindByClass(child, className);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}
}
