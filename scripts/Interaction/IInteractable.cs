using Godot;

namespace RPG.Interaction;

public interface IInteractable
{
	string GetPrompt();
	void Interact(Node interactor);
}
