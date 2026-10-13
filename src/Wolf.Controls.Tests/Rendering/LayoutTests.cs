using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls.Rendering;

namespace Wolf.Controls.Tests.Rendering;

public sealed class LayoutTests
{
    [Fact]
    public void Column_allocates_fixed_and_weighted_regions_in_child_order()
    {
        var layout = new ColumnLayout(6,
            new(Content("H"), LayoutSize.Fixed(1)),
            new(Content("A\nA\nA\nA"), LayoutSize.Weighted()),
            new(Content("S"), LayoutSize.Fixed(1)));
        Lines(layout, 5).Should().Equal("H    ", "A    ", "A    ", "A    ", "A    ", "S    ");
    }

    [Fact]
    public void Overlay_alignment_applies_margins_and_clips_to_canvas()
    {
        var layer = OverlayLayout.Place(Content("P\nP\nP"), 8, 5,
            OverlayAlignment.Bottom, width: 4, height: 2, bottom: 1, zIndex: 9);
        layer.X.Should().Be(2);
        layer.Y.Should().Be(2);
        Lines(new LayeredRenderable(8, 5, [layer]), 8).Should().Equal("        ", "        ", "  P     ", "  P     ", "        ");
    }

    private static string[] Lines(IRenderable renderable, int width = 80)
    {
        var result = new List<string>();
        var line = new System.Text.StringBuilder();
        foreach (var segment in renderable.Render(null!, width))
        {
            if (segment.IsLineBreak) { result.Add(line.ToString()); line.Clear(); }
            else line.Append(segment.Text);
        }
        result.Add(line.ToString());
        return result.ToArray();
    }

    private static IRenderable Content(string value) => new PlainContent(value);

    private sealed class PlainContent(string value) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => new(1, maxWidth);
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
            value.Split('\n').SelectMany((line, index) => index == 0
                ? [new Segment(line)]
                : new[] { Segment.LineBreak, new Segment(line) });
    }
}
