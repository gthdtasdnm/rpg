using Godot;
using RPG.Data;
using RPG.Interaction;

namespace RPG.Items;

public partial class ItemPickup : StaticBody3D, IInteractable
{
	[Export] public string ItemId = "";
	[Export] public int Amount = 1;

	public string GetPrompt()
	{
		ItemDefinition? definition = GameData.Instance.GetItem(ItemId);
		string name = definition?.Name ?? ItemId;
		return $"E - {name} aufheben";
	}

	public void Interact(Node interactor)
	{
		interactor.GetNode<Inventory>("Inventory").AddItem(ItemId, Amount);
		QueueFree();
	}
}
