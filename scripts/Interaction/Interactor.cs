using Godot;
using RPG.Dialogue;

namespace RPG.Interaction;

// Sitzt als RayCast3D an der Kamera. Meldet per Signal, wenn ein IInteractable
// im Blick ist, und löst es bei der "interact"-Taste aus.
//
// ⚠️ Reichweite (target_position.z): die Kamera steht in 3rd-Person hinter dem Spieler, der
// Strahl muss also erst diese Strecke zuruecklegen, bevor er ueberhaupt vor dem Spieler ankommt.
// Bei 4,5 m Kameraabstand bedeutet -7,5 rund 3 m echte Reichweite vor dem Charakter.
public partial class Interactor : RayCast3D
{
	[Signal] public delegate void TargetChangedEventHandler(string prompt);
	[Signal] public delegate void TargetLostEventHandler();

	private IInteractable? _current;

	public override void _Ready()
	{
		// Sonst trifft der Strahl auf dem Weg von der Kamera nach vorne den Spieler selbst.
		if (Owner is CollisionObject3D self)
			AddException(self);
	}

	public override void _PhysicsProcess(double delta)
	{
		// Waehrend eines Gespraechs nichts anvisieren und nichts ausloesen (sonst startet die
		// Interaktionstaste den gerade laufenden Dialog erneut).
		if (DialogueBridge.Instance.IsActive)
		{
			SetTarget(null);
			return;
		}

		SetTarget(IsColliding() && GetCollider() is IInteractable interactable ? interactable : null);

		if (_current != null && Input.IsActionJustPressed("interact"))
			_current.Interact(Owner);
	}

	private void SetTarget(IInteractable? target)
	{
		if (target == _current)
			return;

		_current = target;
		if (_current != null)
			EmitSignal(SignalName.TargetChanged, _current.GetPrompt());
		else
			EmitSignal(SignalName.TargetLost);
	}
}
