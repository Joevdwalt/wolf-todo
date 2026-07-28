namespace WolfTodo.Tui.Controls;

/// <summary>The next state and semantic result emitted by a component.</summary>
public sealed record TuiComponentTransition<TState, TOutcome>(TState? State, TOutcome Outcome);
