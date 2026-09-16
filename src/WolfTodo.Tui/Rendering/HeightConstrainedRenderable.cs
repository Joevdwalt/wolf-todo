using Spectre.Console;
using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Rendering;

public sealed class HeightConstrainedRenderable(IRenderable content, int height) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => content.Measure(options, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var constrainedHeight = Math.Max(1, height);
        var lines = Segment.SplitLines(content.Render(options, maxWidth), maxWidth, null);
        var hasOverflow = lines.Count > constrainedHeight;
        var visibleLines = lines.Take(constrainedHeight).Select(line => line.ToList()).ToList();

        while (visibleLines.Count < constrainedHeight)
        {
            visibleLines.Add([]);
        }

        if (hasOverflow)
        {
            visibleLines[^1] = WithEllipsis(visibleLines[^1], maxWidth);
        }

        for (var index = 0; index < visibleLines.Count; index++)
        {
            if (visibleLines[index].Count == 0)
            {
                yield return Segment.Padding(1);
            }

            foreach (var segment in visibleLines[index])
            {
                yield return segment;
            }

            if (index < visibleLines.Count - 1)
            {
                yield return Segment.LineBreak;
            }
        }
    }

    private static List<Segment> WithEllipsis(IReadOnlyList<Segment> line, int maxWidth)
    {
        if (maxWidth <= 1)
        {
            return [new Segment("…", LastStyle(line), LastLink(line))];
        }

        var result = Segment.Truncate(line, maxWidth - 1).ToList();
        result.Add(new Segment("…", LastStyle(result.Count == 0 ? line : result), LastLink(result.Count == 0 ? line : result)));
        return result;
    }

    private static Style LastStyle(IReadOnlyList<Segment> segments) =>
        segments.LastOrDefault(segment => !segment.IsLineBreak)?.Style ?? Style.Plain;

    private static Link? LastLink(IReadOnlyList<Segment> segments) =>
        segments.LastOrDefault(segment => !segment.IsLineBreak)?.Link;
}
