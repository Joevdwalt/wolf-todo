using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls;

/// <summary>A host-timed indeterminate spinner using a Braille frame sequence.</summary>
public sealed class Spinner : IAnimatedControl<SpinnerState>
{
    private static readonly string[] Frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    public static Spinner Default { get; } = new();

    public IRenderable Render(SpinnerState state, ControlTheme theme, ControlConstraints constraints, DateTimeOffset now) =>
        new Text($"{FrameAt(now)} {state.Label}", new Style(theme.AccentBright, decoration: Decoration.Bold)).Ellipsis();

    public DateTimeOffset? NextFrameAt(SpinnerState state, DateTimeOffset now) => now + AnimationTiming.FrameInterval;

    public static string FrameAt(DateTimeOffset now) =>
        Frames[(int)((now.ToUnixTimeMilliseconds() / (long)AnimationTiming.FrameInterval.TotalMilliseconds) % Frames.Length)];
}
