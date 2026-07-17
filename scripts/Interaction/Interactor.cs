using Godot;

namespace RPG.Interaction;

// Sitzt als RayCast3D an der Kamera. Meldet per Signal, wenn ein IInteractable
// im Blick ist, und löst es bei der "interact"-Taste aus.
public partial class Interactor : RayCast3D
{
	[Signal] public delegate void TargetChangedEventHandler(string prompt);
	[Signal] public delegate void TargetLostEventHandler();

	private IInteractable? _current;

	public override void _PhysicsProcess(double delta)
	{
		IInteractable? hit = IsColliding() && GetCollider() is IInteractable interactable ? interactable : null;

		if (hit != _current)
		{
			_current = hit;
			if (_current != null)
				EmitSignal(SignalName.TargetChanged, _current.GetPrompt());
			else
				EmitSignal(SignalName.TargetLost);
		}

		if (_current != null && Input.IsActionJustPressed("interact"))
			_current.Interact(Owner);
	}
}
