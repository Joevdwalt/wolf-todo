using Spectre.Console;
using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Infrastructure;

internal sealed class SurfaceRenderable(
    IRenderable content,
    Color background,
    bool expand = false) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        var measurement = content.Measure(options, maxWidth);
        return expand ? new Measurement(maxWidth, maxWidth) : measurement;
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var lineWidth = 0;
        var endedWithLineBreak = false;
        foreach (var segment in content.Render(options, maxWidth))
        {
            if (segment.IsLineBreak)
            {
                if (expand && lineWidth < maxWidth)
                {
                    yield return Padding(maxWidth - lineWidth);
                }

                yield return segment;
                lineWidth = 0;
                endedWithLineBreak = true;
                continue;
            }

            lineWidth += segment.CellCount();
            endedWithLineBreak = false;
            yield return new Segment(
                segment.Text,
                WithBackground(segment.Style),
                segment.Link);
        }

        if (expand && !endedWithLineBreak && lineWidth < maxWidth)
        {
            yield return Padding(maxWidth - lineWidth);
        }
    }

    private Segment Padding(int width) => new(
        new string(' ', width),
        new Style(background: background),
        null);

    private Style WithBackground(Style style) =>
        style.Background == Color.Default
            ? new Style(style.Foreground, background, style.Decoration)
            : style;
}
