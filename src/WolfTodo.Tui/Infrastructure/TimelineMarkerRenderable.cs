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

        var label = "┣━━ NOW ";
        var text = maxWidth <= label.Length
            ? label[..maxWidth]
            : label + new string('━', maxWidth - label.Length);
        yield return new Segment(text, style, null);
    }
}
