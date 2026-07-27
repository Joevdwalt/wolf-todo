using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

/// <summary>
/// A terminal control that owns its presentation, size, and input transition.
/// Feature reducers map the returned semantic outcome to application behavior.
/// </summary>
public interface ITuiComponent<TState, TOutcome>
{
    TuiComponentTransition<TState, TOutcome> Reduce(
        TState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings);

    int Measure(TState state, TuiComponentConstraints constraints);

    IRenderable Render(TState state, TuiTheme theme, TuiComponentConstraints constraints);
}

/// <summary>Terminal space made available to a component by its parent layout.</summary>
public sealed record TuiComponentConstraints(int Width, int MaxRows)
{
    public int ClampedWidth => Math.Max(1, Width);

    public int ClampedMaxRows => Math.Max(1, MaxRows);
}

/// <summary>The next state and semantic result emitted by a component.</summary>
public sealed record TuiComponentTransition<TState, TOutcome>(TState? State, TOutcome Outcome);
