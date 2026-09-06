using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls;

/// <summary>A host-timed determinate progress bar with a short linear value transition.</summary>
public sealed class ProgressBar : IAnimatedControl<ProgressState>
{
    public static ProgressBar Default { get; } = new();

    public IRenderable Render(ProgressState state, ControlTheme theme, ControlConstraints constraints, DateTimeOffset now)
    {
        var width = Math.Max(1, constraints.ClampedWidth - state.Label.Length - 9);
        var value = ValueAt(state, now);
        var filled = (int)Math.Round(width * value, MidpointRounding.AwayFromZero);
        var text = $"{state.Label} [{new string('█', filled)}{new string('░', width - filled)}] {(int)Math.Round(value * 100):00}%";
        return new Text(text, new Style(theme.Accent));
    }

    public DateTimeOffset? NextFrameAt(ProgressState state, DateTimeOffset now) =>
        now >= state.ChangedAt + AnimationTiming.TransitionDuration ? null : now + AnimationTiming.FrameInterval;

    public static double ValueAt(ProgressState state, DateTimeOffset now)
    {
        var elapsed = Math.Clamp((now - state.ChangedAt).TotalMilliseconds / AnimationTiming.TransitionDuration.TotalMilliseconds, 0, 1);
        return Math.Clamp(state.PreviousValue + ((state.Value - state.PreviousValue) * elapsed), 0, 1);
    }
}
