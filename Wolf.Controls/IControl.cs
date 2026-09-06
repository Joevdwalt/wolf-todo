using Spectre.Console.Rendering;

namespace Wolf.Controls;

/// <summary>A reusable terminal control with typed state, input, and outcomes.</summary>
public interface IControl<TState, TOutcome>
{
    ControlTransition<TState, TOutcome> Reduce(TState state, ControlInput input);

    int Measure(TState state, ControlConstraints constraints);

    IRenderable Render(TState state, ControlTheme theme, ControlConstraints constraints);
}
