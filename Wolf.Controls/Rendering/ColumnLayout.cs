using Spectre.Console.Rendering;

namespace Wolf.Controls.Rendering;

/// <summary>Places children top to bottom, allocating fixed sizes before weighted shares.</summary>
public sealed class ColumnLayout(int height, params LayoutChild[] children) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => new(maxWidth, maxWidth);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        new LayoutRenderable(children, false, height).Render(options, maxWidth);
}
