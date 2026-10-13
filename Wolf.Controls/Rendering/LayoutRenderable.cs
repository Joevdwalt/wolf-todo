using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls.Rendering;

internal sealed class LayoutRenderable(IReadOnlyList<LayoutChild> children, bool horizontal, int availableHeight) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => new(maxWidth, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Max(1, maxWidth);
        var height = Math.Max(1, availableHeight);
        var extent = horizontal ? width : height;
        var fixedTotal = children.Where(c => !c.Size.IsWeighted).Sum(c => c.Size.Value);
        var weightTotal = children.Where(c => c.Size.IsWeighted).Sum(c => c.Size.Value);
        var remaining = Math.Max(0, extent - fixedTotal);
        var y = 0;
        var x = 0;
        var layers = new List<ControlLayer>();
        foreach (var child in children)
        {
            var size = child.Size.IsWeighted
                ? (weightTotal == 0 ? 0 : remaining * child.Size.Value / weightTotal)
                : child.Size.Value;
            if (horizontal)
                layers.Add(new ControlLayer(new HeightClipRenderable(child.Content, height), x, 0, Width: size));
            else
                layers.Add(new ControlLayer(new HeightClipRenderable(child.Content, size), 0, y, Width: width));
            if (horizontal) x += size; else y += size;
        }
        return new LayeredRenderable(width, height, layers).Render(options, maxWidth);
    }
}

internal sealed class HeightClipRenderable(IRenderable content, int height) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => content.Measure(options, maxWidth);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        Segment.SplitLines(content.Render(options, maxWidth), maxWidth, null)
            .Take(Math.Max(0, height)).SelectMany((line, i) => i == 0
                ? line
                : new[] { Segment.LineBreak }.Concat(line));
}
