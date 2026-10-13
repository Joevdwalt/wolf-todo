using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls.Rendering;

namespace Wolf.Controls.Tests.Rendering;

public sealed class LayeredRenderableTests
{
    [Fact]
    public void Spectre_console_renders_overlapping_controls()
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 8;

        console.Write(new LayeredRenderable(8, 2,
        [
            new ControlLayer(new Text("abcdefgh"), 0, 0, Width: 8),
            new ControlLayer(new Text("TOP"), 2, 0, 1)
        ]));

        writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[0]
            .Should().Be("abTOPfgh");
    }

    [Fact]
    public void Higher_z_index_wins_regardless_of_input_order()
    {
        var canvas = new LayeredRenderable(5, 2,
        [
            new ControlLayer(Content("TOP"), 1, 0, 10),
            new ControlLayer(Content("abcde"), 0, 0, -2)
        ]);

        Lines(canvas).Should().Equal("aTOPe", "     ");
    }

    [Fact]
    public void Later_layer_wins_at_the_same_z_index_and_preserves_style()
    {
        var front = new Style(Color.Red);
        var canvas = new LayeredRenderable(3, 1,
        [
            new ControlLayer(Content("abc", new Style(Color.Blue)), 0, 0),
            new ControlLayer(Content("X", front), 1, 0)
        ]);

        var segments = canvas.Render(null!, 3).Where(segment => !segment.IsLineBreak).ToArray();
        string.Concat(segments.Select(segment => segment.Text)).Should().Be("aXc");
        segments[1].Style.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void Clips_layers_to_canvas_and_preserves_underlying_uncovered_cells()
    {
        var canvas = new LayeredRenderable(4, 2,
        [
            new ControlLayer(Content("abcd\nefgh"), 0, 0),
            new ControlLayer(Content("XY\nZQ"), 3, 0, 1, 2)
        ]);

        Lines(canvas).Should().Equal("abcX", "efgZ");
    }

    [Fact]
    public void Replacing_one_cell_of_a_wide_glyph_clears_the_whole_glyph()
    {
        var canvas = new LayeredRenderable(4, 1,
        [
            new ControlLayer(Content("狼a"), 0, 0),
            new ControlLayer(Content("X"), 1, 0, 1)
        ]);

        Lines(canvas).Should().Equal(" Xa ");
    }

    private static string[] Lines(IRenderable renderable)
    {
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var segment in renderable.Render(null!, 80))
        {
            if (segment.IsLineBreak)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(segment.Text);
            }
        }

        lines.Add(current.ToString());
        return lines.ToArray();
    }

    private static IRenderable Content(string value, Style? style = null) =>
        new SegmentsRenderable(value.Split('\n').SelectMany((line, index) =>
            index == 0
                ? [new Segment(line, style ?? Style.Plain)]
                : new[] { Segment.LineBreak, new Segment(line, style ?? Style.Plain) }).ToArray());

    private sealed class SegmentsRenderable(params Segment[] segments) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => new(1, maxWidth);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => segments;
    }
}
