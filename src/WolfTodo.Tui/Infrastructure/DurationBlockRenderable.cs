using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure;

internal sealed class DurationBlockRenderable(
    IRenderable content,
    DurationBlockPosition position,
    Style borderStyle,
    bool isCursor) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => new(maxWidth, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            yield break;
        }

        if (maxWidth < 5)
        {
            yield return new Segment(new string('─', maxWidth), borderStyle);
            yield break;
        }

        if (position == DurationBlockPosition.End)
        {
            yield return RoundedBase(maxWidth, isCursor);
            yield break;
        }

        if (position == DurationBlockPosition.Middle)
        {
            yield return VerticalRail(maxWidth, isCursor);
            yield break;
        }

        foreach (var segment in Header(content, options, maxWidth))
        {
            yield return segment;
        }
    }

    private IEnumerable<Segment> Header(IRenderable content, RenderOptions options, int maxWidth)
    {
        var contentWidth = maxWidth - 1;
        var segments = Segment.Truncate(content.Render(options, contentWidth), contentWidth).ToArray();
        var padding = Math.Max(0, contentWidth - Segment.CellCount(segments));

        foreach (var segment in segments)
        {
            yield return segment;
        }

        if (padding > 0)
        {
            yield return new Segment(new string('─', padding), borderStyle);
        }

        yield return new Segment("╮", borderStyle);
    }

    private Segment VerticalRail(int maxWidth, bool isCursor) =>
        new($"{CursorPrefix(isCursor)}│{new string(' ', maxWidth - 4)}│", borderStyle);

    private Segment RoundedBase(int maxWidth, bool isCursor) =>
        new($"{CursorPrefix(isCursor)}╰{new string('─', maxWidth - 4)}╯", borderStyle);

    private static string CursorPrefix(bool isCursor) => isCursor ? "> " : "  ";
}
