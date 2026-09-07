using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls.Splash;

/// <summary>A panel that expands from a small centered box to its available terminal area.</summary>
public sealed class SplashBox : IAnimatedControl<SplashBoxState>
{
    public static TimeSpan ExpansionDuration { get; } = TimeSpan.FromMilliseconds(800);

    public static TimeSpan FrameInterval { get; } = TimeSpan.FromMilliseconds(33);

    public static SplashBox Default { get; } = new();

    public IRenderable Render(SplashBoxState state, ControlTheme theme, ControlConstraints constraints, DateTimeOffset now)
    {
        var targetWidth = constraints.ClampedWidth;
        var targetHeight = constraints.ClampedMaxRows;
        if (targetWidth < 3 || targetHeight < 3)
        {
            return new Align(new Text(state.Title, new Style(theme.AccentBright, decoration: Decoration.Bold)), HorizontalAlignment.Center);
        }

        var size = SizeAt(state, constraints, now);
        var panel = new Panel(CreateContent(state, size.Width, size.Height, theme, ContentVisibleAt(state, now)))
        {
            Border = BoxBorder.Square,
            BorderStyle = new Style(theme.BorderActive),
            Padding = new Padding(0),
            Width = size.Width,
            Height = size.Height,
            Expand = false
        };

        var topPadding = (targetHeight - size.Height) / 2;
        var bottomPadding = targetHeight - size.Height - topPadding;
        var rows = new List<IRenderable>();
        for (var index = 0; index < topPadding; index++)
        {
            rows.Add(BlankLine(targetWidth));
        }

        rows.Add(new Align(panel, HorizontalAlignment.Center));

        for (var index = 0; index < bottomPadding; index++)
        {
            rows.Add(BlankLine(targetWidth));
        }

        return new Rows(rows);
    }

    public DateTimeOffset? NextFrameAt(SplashBoxState state, DateTimeOffset now)
    {
        var completion = state.StartedAt + ExpansionDuration;
        if (now >= completion)
        {
            return null;
        }

        var nextFrame = now + FrameInterval;
        return nextFrame > completion ? completion : nextFrame;
    }

    public static double ExpansionAt(SplashBoxState state, DateTimeOffset now)
    {
        var linear = Math.Clamp((now - state.StartedAt).TotalMilliseconds / ExpansionDuration.TotalMilliseconds, 0, 1);
        return 1 - Math.Pow(1 - linear, 3);
    }

    public static bool ContentVisibleAt(SplashBoxState state, DateTimeOffset now) =>
        now >= state.StartedAt + ExpansionDuration;

    public static SplashBoxSize SizeAt(SplashBoxState state, ControlConstraints constraints, DateTimeOffset now)
    {
        var progress = ExpansionAt(state, now);
        var targetWidth = constraints.ClampedWidth;
        var targetHeight = constraints.ClampedMaxRows;
        return new(
            Interpolate(Math.Min(12, targetWidth), targetWidth, progress),
            Interpolate(Math.Min(3, targetHeight), targetHeight, progress));
    }

    private static IRenderable CreateContent(
        SplashBoxState state,
        int width,
        int height,
        ControlTheme theme,
        bool showContent)
    {
        var innerHeight = Math.Max(1, height - 2);
        var lines = Enumerable.Repeat<IRenderable>(BlankLine(Math.Max(1, width - 2)), innerHeight).ToList();
        if (!showContent)
        {
            return new Rows(lines);
        }

        var titleIndex = state.Subtitle is not null && innerHeight >= 2 ? Math.Max(0, (innerHeight / 2) - 1) : innerHeight / 2;
        lines[titleIndex] = new Align(new Text(state.Title, new Style(theme.AccentBright, decoration: Decoration.Bold)), HorizontalAlignment.Center);

        if (state.Subtitle is not null && innerHeight >= 2)
        {
            lines[Math.Min(innerHeight - 1, titleIndex + 1)] = new Align(
                new Text(state.Subtitle, new Style(theme.Muted, decoration: Decoration.Dim)).Ellipsis(),
                HorizontalAlignment.Center);
        }

        return new Rows(lines);
    }

    private static int Interpolate(int start, int end, double progress) =>
        (int)Math.Round(start + ((end - start) * progress), MidpointRounding.AwayFromZero);

    private static Text BlankLine(int width) => new(new string(' ', Math.Max(1, width)));
}
