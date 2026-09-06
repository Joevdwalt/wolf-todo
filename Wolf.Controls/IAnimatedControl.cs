using Spectre.Console.Rendering;

namespace Wolf.Controls;

/// <summary>A render-only control whose frames are advanced by a host-supplied clock.</summary>
public interface IAnimatedControl<TState>
{
    IRenderable Render(TState state, ControlTheme theme, ControlConstraints constraints, DateTimeOffset now);

    DateTimeOffset? NextFrameAt(TState state, DateTimeOffset now);
}
