using Godot;
using RPG.Data;
using RPG.Interaction;

namespace RPG.Items;

// Ein Item, das in der Welt liegt (Objects/world_item.tscn).
//
// Platzieren heißt: Szene einfügen, `ItemId` eintragen, hinstellen - Modell und Kollision baut
// dieser Knoten selbst aus der ItemDefinition. Weil das Script ein @tool ist, passiert das auch
// im Editor, man sieht also sofort, was man da hinlegt.
//
// Die erzeugten Kinder bekommen bewusst keinen Owner und landen deshalb nicht in der .tscn -
// gespeichert wird nur der Knoten selbst mit seiner ItemId.
[Tool]
public partial class ItemPickup : StaticBody3D, IInteractable
{
	// Ab diesem Verhältnis von Höhe zu Grundfläche gilt ein Modell als länglich und wird
	// hingelegt statt aufgestellt - Schwerter und Stäbe stehen sonst wie gepflanzt im Boden.
	private const float LyingRatio = 1.6f;

	// Kleine Gegenstände sollen trotzdem sicher vom Interaktionsstrahl getroffen werden.
	private const float MinCollisionSize = 0.35f;

	private string _itemId = "";
	private Node3D? _model;
	private CollisionShape3D? _collision;

	[Export]
	public string ItemId
	{
		get => _itemId;
		set
		{
			_itemId = value;
			if (IsNodeReady())
				Rebuild();
		}
	}

	[Export] public int Amount = 1;

	// Aus: Modell so lassen, wie es im Ursprung steht. Für Items, bei denen die Automatik
	// danebenliegt.
	[Export] public bool AutoLayDown = true;

	public override void _Ready() => Rebuild();

	public string GetPrompt()
	{
		ItemDefinition? item = GameData.LookupItem(ItemId);
		string name = item?.Name ?? ItemId;
		return Amount > 1 ? $"E - {name} ×{Amount} aufheben" : $"E - {name} aufheben";
	}

	public void Interact(Node interactor)
	{
		interactor.GetNode<Inventory>("Inventory").AddItem(ItemId, Amount);
		QueueFree();
	}

	private void Rebuild()
	{
		DiscardGenerated();

		ItemDefinition? item = GameData.LookupItem(ItemId);
		if (item == null)
		{
			if (!string.IsNullOrWhiteSpace(ItemId))
				GD.PushWarning($"ItemPickup: unbekannte ItemId '{ItemId}'");
			return;
		}

		_model = ItemModel.Instantiate(item);
		if (_model != null)
			AddChild(_model);

		Aabb bounds = PlaceOnGround();
		AddCollision(bounds);
	}

	// Legt längliche Sachen hin und schiebt das Modell so, dass seine Unterkante auf dem Ursprung
	// des Knotens liegt. Dadurch reicht es beim Platzieren, den Knoten auf den Boden zu setzen.
	private Aabb PlaceOnGround()
	{
		if (_model == null)
			return new Aabb(Vector3.One * (-MinCollisionSize / 2f), Vector3.One * MinCollisionSize);

		Transform3D toSelf = GlobalTransform.AffineInverse();
		Aabb bounds = ItemModel.ComputeBounds(_model, toSelf);

		if (AutoLayDown && bounds.Size.Y > LyingRatio * Mathf.Max(bounds.Size.X, bounds.Size.Z))
		{
			_model.RotateX(-Mathf.Pi / 2f);
			bounds = ItemModel.ComputeBounds(_model, toSelf);
		}

		float lift = -bounds.Position.Y;
		_model.Position += new Vector3(0f, lift, 0f);
		return new Aabb(bounds.Position + new Vector3(0f, lift, 0f), bounds.Size);
	}

	private void AddCollision(Aabb bounds)
	{
		Vector3 size = new(
			Mathf.Max(bounds.Size.X, MinCollisionSize),
			Mathf.Max(bounds.Size.Y, MinCollisionSize),
			Mathf.Max(bounds.Size.Z, MinCollisionSize));

		_collision = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = size },
			Position = bounds.GetCenter(),
		};
		AddChild(_collision);
	}

	private void DiscardGenerated()
	{
		// Erst aushängen, dann freigeben: QueueFree wirkt verzögert, und bis dahin würde das alte
		// Modell im Editor neben dem neuen stehen.
		if (_model != null)
		{
			RemoveChild(_model);
			_model.QueueFree();
			_model = null;
		}

		if (_collision != null)
		{
			RemoveChild(_collision);
			_collision.QueueFree();
			_collision = null;
		}
	}
}
