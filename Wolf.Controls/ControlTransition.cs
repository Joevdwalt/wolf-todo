namespace Wolf.Controls;

/// <summary>The next state and semantic result emitted by an input control.</summary>
public sealed record ControlTransition<TState, TOutcome>(TState? State, TOutcome Outcome);
