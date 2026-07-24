using Godot;
using System.Collections.Generic;

namespace RPG.Tools;

// Editor-Tool: verteilt Baum-Szenen per Maske auf einem Terrain und backt das Ergebnis als
// MultiMeshInstance3D-Nodes fest in die Szene. Läuft nur über die Inspector-Buttons, nie zur Laufzeit.
[Tool]
public partial class ForestPainter : Node3D
{
	private const string GeneratedForestName = "GeneratedForest";

	[ExportGroup("Input")]
	[Export] public MeshInstance3D TerrainMesh = null!;
	[Export] public Texture2D ForestMask = null!;
	[Export] public Godot.Collections.Array<PackedScene> TreeScenes = new();

	[ExportGroup("Placement")]
	[Export(PropertyHint.Range, "0.2,20,0.1")] public float GridSpacing = 2.0f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float PlacementProbability = 0.6f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float JitterAmount = 0.9f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float MaskThreshold = 0.5f;
	[Export] public bool FlipMaskX = false;
	[Export] public bool FlipMaskZ = true;
	[Export] public bool SwapAxes = false;
	[Export] public int RandomSeed = 12345;

	[ExportGroup("Look Variation")]
	[Export] public float MinScale = 0.85f;
	[Export] public float MaxScale = 1.15f;

	[ExportGroup("Raycast")]
	[Export] public float RaycastMargin = 50f;
	[Export(PropertyHint.Layers3DPhysics)] public uint TerrainCollisionMask = 1;

	[ExportGroup("Performance")]
	[Export(PropertyHint.Range, "5,500,1")] public float ChunkSize = 100f;
	[Export] public float RenderDistance = 0f;
	[Export] public int SafetyTreeLimit = 150000;
	[Export] public long MaxCandidateScan = 4_000_000;

	[ExportGroup("Actions")]
	[ExportToolButton("Bake Forest")]
	public Callable BakeButton => Callable.From(BakeForest);

	[ExportToolButton("Clear Forest")]
	public Callable ClearButton => Callable.From(ClearForest);

	private class TreePart
	{
		public Mesh Mesh = null!;
		public Transform3D LocalTransform;
	}

	private class TreeSpecies
	{
		public string Name = "";
		public readonly List<TreePart> Parts = new();
	}

	public void ClearForest()
	{
		var container = GetNodeOrNull<Node3D>(GeneratedForestName);
		if (container == null)
			return;

		foreach (Node child in container.GetChildren())
			child.Free();

		GD.Print("ForestPainter: Wald geleert.");
	}

	public void BakeForest()
	{
		if (!Engine.IsEditorHint())
		{
			GD.PrintErr("ForestPainter: Bake läuft nur im Editor.");
			return;
		}
		if (TerrainMesh?.Mesh == null)
		{
			GD.PrintErr("ForestPainter: TerrainMesh nicht gesetzt.");
			return;
		}
		if (ForestMask == null)
		{
			GD.PrintErr("ForestPainter: ForestMask nicht gesetzt.");
			return;
		}
		if (TreeScenes.Count == 0)
		{
			GD.PrintErr("ForestPainter: Keine TreeScenes zugewiesen.");
			return;
		}

		Node? sceneRoot = GetTree()?.EditedSceneRoot;
		if (sceneRoot == null)
		{
			GD.PrintErr("ForestPainter: Keine editierte Szene gefunden (Szene im Editor öffnen).");
			return;
		}

		ClearForest();
		var container = GetNodeOrNull<Node3D>(GeneratedForestName);
		if (container == null)
		{
			container = new Node3D { Name = GeneratedForestName };
			AddChild(container);
			container.Owner = sceneRoot;
		}

		// 1. Terrain-Ausdehnung aus der Mesh-AABB in Weltkoordinaten (nicht hardcoden).
		Aabb localAabb = TerrainMesh.Mesh.GetAabb();
		Transform3D terrainXform = TerrainMesh.GlobalTransform;
		Vector3 min = terrainXform * localAabb.GetEndpoint(0);
		Vector3 max = min;
		for (int i = 1; i < 8; i++)
		{
			Vector3 corner = terrainXform * localAabb.GetEndpoint(i);
			min = new Vector3(Mathf.Min(min.X, corner.X), Mathf.Min(min.Y, corner.Y), Mathf.Min(min.Z, corner.Z));
			max = new Vector3(Mathf.Max(max.X, corner.X), Mathf.Max(max.Y, corner.Y), Mathf.Max(max.Z, corner.Z));
		}
		GD.Print($"ForestPainter: Terrain-AABB (Welt) min={min} max={max} Mitte=({(min.X + max.X) / 2f:F1}, {(min.Z + max.Z) / 2f:F1})");

		// 2. Maske als rohes Pixel-Bild laden (Texture-Filter beeinflusst das nicht, nur die Darstellung).
		Image? mask = ForestMask.GetImage();
		if (mask == null)
		{
			GD.PrintErr("ForestPainter: Konnte Bilddaten der Maske nicht lesen.");
			return;
		}
		if (mask.IsCompressed())
			mask.Decompress();
		int maskW = mask.GetWidth();
		int maskH = mask.GetHeight();

		float sizeX = max.X - min.X;
		float sizeZ = max.Z - min.Z;

		int stepsX = Mathf.Max(1, Mathf.FloorToInt(sizeX / GridSpacing) + 1);
		int stepsZ = Mathf.Max(1, Mathf.FloorToInt(sizeZ / GridSpacing) + 1);
		long estimatedCandidates = (long)stepsX * stepsZ;
		GD.Print($"ForestPainter: Terrain ist {sizeX:F0} x {sizeZ:F0} m groß -> {estimatedCandidates} Kandidatenpunkte bei GridSpacing={GridSpacing}.");
		if (estimatedCandidates > MaxCandidateScan)
		{
			GD.PrintErr($"ForestPainter: {estimatedCandidates} Kandidatenpunkte wären zu viele (Terrain {sizeX:F0} x {sizeZ:F0} m). GridSpacing erhöhen oder MaxCandidateScan anheben.");
			return;
		}

		// 3. Baum-Arten einmalig instanziieren, Meshes + lokale Transforms relativ zum Szenen-Root einsammeln.
		var species = new List<TreeSpecies>();
		foreach (PackedScene? scene in TreeScenes)
		{
			if (scene == null)
				continue;

			Node3D inst = scene.Instantiate<Node3D>();
			var sp = new TreeSpecies { Name = scene.ResourcePath.GetFile() };
			CollectMeshParts(inst, inst, sp.Parts);
			inst.Free();

			if (sp.Parts.Count > 0)
				species.Add(sp);
		}
		if (species.Count == 0)
		{
			GD.PrintErr("ForestPainter: Keine MeshInstance3D in den TreeScenes gefunden.");
			return;
		}

		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)RandomSeed;

		PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;

		// Platzierungen werden nach Chunk-Zelle UND Baumart gebündelt, damit jeder Chunk später
		// ein eigenes, klein-räumiges MultiMeshInstance3D bekommt (echtes Frustum-/Distanz-Culling pro Chunk).
		var chunkPlacements = new Dictionary<Vector2I, List<Transform3D>[]>();

		float rayFrom = max.Y + RaycastMargin;
		float rayTo = min.Y - RaycastMargin;

		int totalPlaced = 0;
		int rayAttempts = 0;
		int rayHits = 0;
		Vector3 hitMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 hitMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		bool limitReached = false;

		// 4./5. Gitter über die Terrain-Fläche, an weißen Maskenstellen Position + Bodenhöhe bestimmen.
		for (int ix = 0; ix <= stepsX && !limitReached; ix++)
		{
			float x = min.X + ix * GridSpacing;
			for (int iz = 0; iz <= stepsZ; iz++)
			{
				float z = min.Z + iz * GridSpacing;

				float u = sizeX > 0f ? (x - min.X) / sizeX : 0f;
				float v = sizeZ > 0f ? (z - min.Z) / sizeZ : 0f;
				if (FlipMaskX) u = 1f - u;
				if (FlipMaskZ) v = 1f - v;
				if (SwapAxes) (u, v) = (v, u);

				int px = Mathf.Clamp((int)(u * maskW), 0, maskW - 1);
				int py = Mathf.Clamp((int)(v * maskH), 0, maskH - 1);

				Color maskColor = mask.GetPixel(px, py);
				float brightness = (maskColor.R + maskColor.G + maskColor.B) / 3f;
				if (brightness < MaskThreshold)
					continue;

				if (rng.Randf() > PlacementProbability)
					continue;

				float halfJitter = JitterAmount * GridSpacing * 0.5f;
				float jitterX = (rng.Randf() * 2f - 1f) * halfJitter;
				float jitterZ = (rng.Randf() * 2f - 1f) * halfJitter;
				float worldX = x + jitterX;
				float worldZ = z + jitterZ;

				var query = PhysicsRayQueryParameters3D.Create(
					new Vector3(worldX, rayFrom, worldZ),
					new Vector3(worldX, rayTo, worldZ),
					TerrainCollisionMask);
				rayAttempts++;
				Godot.Collections.Dictionary result = spaceState.IntersectRay(query);
				if (result.Count == 0)
					continue;

				var hitPos = (Vector3)result["position"];
				rayHits++;
				hitMin = new Vector3(Mathf.Min(hitMin.X, hitPos.X), Mathf.Min(hitMin.Y, hitPos.Y), Mathf.Min(hitMin.Z, hitPos.Z));
				hitMax = new Vector3(Mathf.Max(hitMax.X, hitPos.X), Mathf.Max(hitMax.Y, hitPos.Y), Mathf.Max(hitMax.Z, hitPos.Z));

				float angle = rng.Randf() * Mathf.Tau;
				float scale = rng.RandfRange(MinScale, MaxScale);
				var basis = Basis.Identity.Rotated(Vector3.Up, angle).Scaled(Vector3.One * scale);

				int speciesIdx = Mathf.Min(species.Count - 1, (int)(rng.Randf() * species.Count));

				int chunkX = Mathf.FloorToInt((worldX - min.X) / ChunkSize);
				int chunkZ = Mathf.FloorToInt((worldZ - min.Z) / ChunkSize);
				var chunkKey = new Vector2I(chunkX, chunkZ);
				if (!chunkPlacements.TryGetValue(chunkKey, out List<Transform3D>[]? perSpeciesLists))
				{
					perSpeciesLists = new List<Transform3D>[species.Count];
					for (int i = 0; i < species.Count; i++)
						perSpeciesLists[i] = new List<Transform3D>();
					chunkPlacements[chunkKey] = perSpeciesLists;
				}
				perSpeciesLists[speciesIdx].Add(new Transform3D(basis, hitPos));
				totalPlaced++;

				if (totalPlaced >= SafetyTreeLimit)
				{
					GD.PrintErr($"ForestPainter: Sicherheitslimit von {SafetyTreeLimit} Bäumen erreicht, Bake abgebrochen. GridSpacing erhöhen oder Limit anpassen.");
					limitReached = true;
					break;
				}
			}
		}

		if (rayAttempts > 0)
		{
			float hitRate = 100f * rayHits / rayAttempts;
			GD.Print($"ForestPainter: Raycasts {rayHits}/{rayAttempts} Treffer ({hitRate:F1}%).");
			if (rayHits > 0)
				GD.Print($"ForestPainter: Trefferbereich (Welt) min=({hitMin.X:F1}, {hitMin.Z:F1}) max=({hitMax.X:F1}, {hitMax.Z:F1}) — vergleiche das mit der Terrain-AABB oben.");
		}

		// 6. Pro Chunk-Zelle, Baumart und Mesh-Teil (Stamm/Blätter getrennt) ein eigenes MultiMeshInstance3D.
		// Jeder Chunk bekommt eine eng anliegende Custom-AABB -> Godot cullt jeden Chunk einzeln per
		// Frustum, in Editor- und Spiel-Viewport gleichermaßen (kein Zusatzcode nötig).
		Transform3D containerInverse = container.GlobalTransform.AffineInverse();
		int chunkCount = 0;
		int multiMeshCount = 0;
		int minInstances = int.MaxValue;
		int maxInstances = 0;
		long totalInstances = 0;

		foreach (KeyValuePair<Vector2I, List<Transform3D>[]> chunkEntry in chunkPlacements)
		{
			Vector2I chunkKey = chunkEntry.Key;
			List<Transform3D>[] perSpeciesLists = chunkEntry.Value;
			Node3D? chunkNode = null;

			for (int s = 0; s < species.Count; s++)
			{
				List<Transform3D> placements = perSpeciesLists[s];
				if (placements.Count == 0)
					continue;

				TreeSpecies sp = species[s];
				for (int p = 0; p < sp.Parts.Count; p++)
				{
					TreePart part = sp.Parts[p];
					Aabb partLocalAabb = part.Mesh.GetAabb();
					var localXforms = new Transform3D[placements.Count];
					Vector3 aabbMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
					Vector3 aabbMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

					for (int i = 0; i < placements.Count; i++)
					{
						Transform3D worldInstXform = placements[i] * part.LocalTransform;
						Transform3D localXform = containerInverse * worldInstXform;
						localXforms[i] = localXform;

						for (int k = 0; k < 8; k++)
						{
							Vector3 corner = localXform * partLocalAabb.GetEndpoint(k);
							aabbMin = new Vector3(Mathf.Min(aabbMin.X, corner.X), Mathf.Min(aabbMin.Y, corner.Y), Mathf.Min(aabbMin.Z, corner.Z));
							aabbMax = new Vector3(Mathf.Max(aabbMax.X, corner.X), Mathf.Max(aabbMax.Y, corner.Y), Mathf.Max(aabbMax.Z, corner.Z));
						}
					}

					// VisibilityRangeEnd misst die Kamera-Distanz zum Ursprung DIESES Nodes, nicht zu den
					// einzelnen MultiMesh-Instanzen. Ohne Offset läge dieser Ursprung für jeden Chunk am
					// selben Punkt (Position des ForestPainter-Nodes) -> Bäume würden nur sichtbar, wenn man
					// sich diesem einen Punkt nähert, nicht den tatsächlichen Baum-Positionen. Deshalb wird
					// der Node ans AABB-Zentrum verschoben und alle Transforms/die AABB um diesen Offset
					// zurückgerechnet (visuell identisch, aber der Distanz-Ursprung sitzt jetzt bei den Bäumen).
					Vector3 originOffset = (aabbMin + aabbMax) * 0.5f;
					for (int i = 0; i < localXforms.Length; i++)
					{
						Transform3D x = localXforms[i];
						x.Origin -= originOffset;
						localXforms[i] = x;
					}

					var mm = new MultiMesh
					{
						TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
						Mesh = part.Mesh,
						InstanceCount = localXforms.Length,
						CustomAabb = new Aabb(aabbMin - originOffset, aabbMax - aabbMin),
					};
					for (int i = 0; i < localXforms.Length; i++)
						mm.SetInstanceTransform(i, localXforms[i]);

					if (chunkNode == null)
					{
						chunkNode = new Node3D { Name = $"Chunk_{chunkKey.X}_{chunkKey.Y}" };
						container.AddChild(chunkNode);
						chunkNode.Owner = sceneRoot;
						chunkCount++;
					}

					var mmi = new MultiMeshInstance3D
					{
						Name = $"MM_{sp.Name}_{p}",
						Multimesh = mm,
						Position = originOffset,
					};
					if (RenderDistance > 0f)
					{
						mmi.VisibilityRangeEnd = RenderDistance;
						mmi.VisibilityRangeEndMargin = RenderDistance * 0.1f;
						mmi.VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;
					}

					chunkNode.AddChild(mmi);
					mmi.Owner = sceneRoot;

					multiMeshCount++;
					totalInstances += localXforms.Length;
					minInstances = Mathf.Min(minInstances, localXforms.Length);
					maxInstances = Mathf.Max(maxInstances, localXforms.Length);
				}
			}
		}

		float avgInstances = multiMeshCount > 0 ? totalInstances / (float)multiMeshCount : 0f;
		GD.Print($"ForestPainter: {totalPlaced} Bäume, {chunkCount} Chunks (Größe {ChunkSize}m), {multiMeshCount} MultiMeshInstance3D. Instanzen/MultiMesh min={minInstances} max={maxInstances} avg={avgInstances:F1}.");
		GD.Print("ForestPainter: Szene jetzt speichern (Strg+S).");
	}

	// Sammelt alle MeshInstance3D unter root ein, mit ihrem Transform relativ zu root
	// (nicht relativ zur Welt), damit Stamm/Blätter unabhängig von root's eigenem Transform stimmen.
	private static void CollectMeshParts(Node3D node, Node3D root, List<TreePart> parts)
	{
		if (node is MeshInstance3D mi && mi.Mesh != null)
		{
			Mesh meshToUse = mi.Mesh;
			int surfaceCount = mi.Mesh.GetSurfaceCount();
			for (int i = 0; i < surfaceCount; i++)
			{
				Material? overrideMat = mi.GetSurfaceOverrideMaterial(i);
				if (overrideMat == null)
					continue;

				if (meshToUse == mi.Mesh)
					meshToUse = (Mesh)mi.Mesh.Duplicate();
				if (meshToUse is ArrayMesh arrayMesh)
					arrayMesh.SurfaceSetMaterial(i, overrideMat);
			}

			parts.Add(new TreePart
			{
				Mesh = meshToUse,
				LocalTransform = GetTransformRelativeTo(mi, root),
			});
		}

		foreach (Node child in node.GetChildren())
		{
			if (child is Node3D child3D)
				CollectMeshParts(child3D, root, parts);
		}
	}

	private static Transform3D GetTransformRelativeTo(Node3D node, Node3D root)
	{
		Transform3D t = Transform3D.Identity;
		Node3D? current = node;
		while (current != null && current != root)
		{
			t = current.Transform * t;
			current = current.GetParentOrNull<Node3D>();
		}
		return t;
	}
}
