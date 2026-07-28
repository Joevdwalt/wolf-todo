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
