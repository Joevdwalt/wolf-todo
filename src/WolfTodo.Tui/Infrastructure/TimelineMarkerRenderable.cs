using Spectre.Console;
using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Infrastructure;

internal sealed class TimelineMarkerRenderable(Style style) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new(maxWidth, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            yield break;
        }

        yield return new Segment(
            $"▶{new string('─', maxWidth - 1)}",
            style,
            null);
    }
}
