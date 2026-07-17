using Godot;
using System.Collections.Generic;

namespace RPG.World;

// Autoload: globaler Flag-Speicher (wie Gothics Quest-/Weltvariablen). Andere Systeme setzen
// Flags bei Ereignissen (NPC angesprochen, Ort betreten, Quest erfüllt, ...) und andere Systeme
// (v.a. Dialoge) koennen sie abfragen, ohne dass die Systeme sich direkt kennen muessen.
public partial class GameFlags : Node
{
	public static GameFlags Instance { get; private set; } = null!;

	[Signal] public delegate void FlagChangedEventHandler(string flagId, bool value);

	private readonly HashSet<string> _flags = new();

	public override void _Ready()
	{
		Instance = this;
	}

	public void SetFlag(string flagId, bool value = true)
	{
		bool changed = value ? _flags.Add(flagId) : _flags.Remove(flagId);
		if (changed)
			EmitSignal(SignalName.FlagChanged, flagId, value);
	}

	public bool HasFlag(string flagId) => _flags.Contains(flagId);

	public IEnumerable<string> GetAllFlags() => _flags;

	public void LoadFlags(IEnumerable<string> flags)
	{
		_flags.Clear();
		foreach (string flag in flags)
			_flags.Add(flag);
	}
}
