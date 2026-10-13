using Spectre.Console.Rendering;

namespace Wolf.Controls.Rendering;

/// <summary>Places children left to right, allocating fixed sizes before weighted shares.</summary>
public sealed class RowLayout : IRenderable
{
    private readonly IReadOnlyList<LayoutChild> children;
    private readonly int height;

    public RowLayout(params LayoutChild[] children) : this(1, children) { }
    public RowLayout(int height, params LayoutChild[] children)
    {
        this.height = Math.Max(1, height);
        this.children = children;
    }

    public Measurement Measure(RenderOptions options, int maxWidth) => new(maxWidth, maxWidth);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        new LayoutRenderable(children, true, height).Render(options, maxWidth);
}
