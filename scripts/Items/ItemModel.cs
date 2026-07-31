using Godot;
using RPG.Data;

namespace RPG.Items;

// Ein Item hat genau EIN Modell (`ItemDefinition.Model`), und das wird an drei Stellen gebraucht:
// es liegt in der Welt (ItemPickup), hängt beim Ausrüsten am Spieler (EquipmentVisuals) und wird
// fürs Inventar-Icon abfotografiert (scripts/UI/ItemIcons.cs). Damit alle drei dasselbe Modell in
// derselben Größe bekommen, laden sie es hierüber.
public static class ItemModel
{
	public static Node3D? Instantiate(ItemDefinition item)
	{
		if (string.IsNullOrWhiteSpace(item.Model))
			return null;

		PackedScene? scene = ResourceLoader.Load<PackedScene>(item.Model);
		if (scene == null)
		{
			GD.PushWarning($"ItemModel: Modell '{item.Model}' von Item '{item.Id}' nicht ladbar");
			return null;
		}

		if (scene.Instantiate() is not Node3D model)
		{
			GD.PushWarning($"ItemModel: Modell '{item.Model}' von Item '{item.Id}' ist kein Node3D");
			return null;
		}

		if (!Mathf.IsEqualApprox(item.ModelScale, 1f))
			model.Scale = Vector3.One * item.ModelScale;

		return model;
	}

	// Umschließende Box über alle sichtbaren Teile eines Teilbaums, umgerechnet in ein beliebiges
	// Bezugssystem: `worldToReference` ist die Inverse der Transformation, in deren Koordinaten das
	// Ergebnis gebraucht wird (Transform3D.Identity liefert Weltkoordinaten).
	//
	// Die Knoten müssen dafür im Baum hängen, sonst hat GlobalTransform keinen Sinn.
	public static Aabb ComputeBounds(Node subtree, Transform3D worldToReference)
	{
		Aabb bounds = default;
		bool hasAny = false;
		Collect(subtree, worldToReference, ref bounds, ref hasAny);
		return hasAny ? bounds : default;
	}

	private static void Collect(Node node, Transform3D worldToReference, ref Aabb bounds, ref bool hasAny)
	{
		if (node is VisualInstance3D visual)
		{
			Aabb local = visual.GetAabb();
			Transform3D toReference = worldToReference * visual.GlobalTransform;

			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = toReference * local.GetEndpoint(i);
				if (hasAny)
				{
					bounds = bounds.Expand(corner);
				}
				else
				{
					bounds = new Aabb(corner, Vector3.Zero);
					hasAny = true;
				}
			}
		}

		foreach (Node child in node.GetChildren())
			Collect(child, worldToReference, ref bounds, ref hasAny);
	}
}
