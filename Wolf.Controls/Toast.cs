using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls;

/// <summary>A transient toast with host-driven horizontal entrance and exit frames.</summary>
public sealed class Toast : IAnimatedControl<ToastState>
{
    public static Toast Default { get; } = new();

    public IRenderable Render(ToastState state, ControlTheme theme, ControlConstraints constraints, DateTimeOffset now)
    {
        if (!state.IsVisibleAt(now))
        {
            return new Text(string.Empty);
        }

        var offset = SlideOffset(state, now);
        var messageWidth = Math.Max(1, constraints.ClampedWidth - offset);
        return new Padder(new Panel(new Text(state.Message, new Style(ColorFor(state.Severity, theme), decoration: Decoration.Bold)).Ellipsis())
        {
            Border = BoxBorder.Square,
            BorderStyle = new Style(ColorFor(state.Severity, theme)),
            Padding = new Padding(1, 0),
            Width = messageWidth,
            Expand = false
        }, new Padding(offset, 0, 0, 0));
    }

    public DateTimeOffset? NextFrameAt(ToastState state, DateTimeOffset now)
    {
        if (!state.IsVisibleAt(now))
        {
            return null;
        }

        var transitionStart = state.VisibleFrom + AnimationTiming.TransitionDuration;
        var transitionEnd = state.VisibleUntil - AnimationTiming.TransitionDuration;
        if (now < transitionStart || now >= transitionEnd)
        {
            return now + AnimationTiming.FrameInterval;
        }

        return transitionEnd;
    }

    private static int SlideOffset(ToastState state, DateTimeOffset now)
    {
        var entering = Math.Clamp((now - state.VisibleFrom).TotalMilliseconds / AnimationTiming.TransitionDuration.TotalMilliseconds, 0, 1);
        var exiting = Math.Clamp((state.VisibleUntil - now).TotalMilliseconds / AnimationTiming.TransitionDuration.TotalMilliseconds, 0, 1);
        return (int)Math.Round(2 * (1 - Math.Min(entering, exiting)), MidpointRounding.AwayFromZero);
    }

    private static Color ColorFor(ToastSeverity severity, ControlTheme theme) => severity switch
    {
        ToastSeverity.Success => theme.Success,
        ToastSeverity.Warning => theme.Warning,
        ToastSeverity.Error => theme.Error,
        _ => theme.Accent
    };
}
