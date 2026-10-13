using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls.Rendering;

namespace Wolf.Controls.Tests.Rendering;

public sealed class SurfaceRenderableTests
{
    [Fact]
    public void Render_paints_unstyled_text_and_width_padding_without_adding_rows()
    {
        var segments = new SurfaceRenderable(new SegmentsRenderable(new Segment("Hi")), Color.Blue, expand: true)
            .Render(null!, 5).ToArray();

        segments.Where(segment => !segment.IsLineBreak).Select(segment => segment.Text)
            .Should().Equal("Hi", "   ");
        segments.Where(segment => !segment.IsLineBreak)
            .Should().OnlyContain(segment => segment.Style.Background == Color.Blue);
        segments.Should().NotContain(segment => segment.IsLineBreak);
    }

    [Fact]
    public void Render_preserves_foreground_and_explicit_nested_background()
    {
        var content = new SegmentsRenderable(new Segment("A", new Style(Color.Red, Color.Green)));
        var segment = new SurfaceRenderable(content, Color.Blue).Render(null!, 4).Single();

        segment.Style.Foreground.Should().Be(Color.Red);
        segment.Style.Background.Should().Be(Color.Green);
    }

    private sealed class SegmentsRenderable(params Segment[] segments) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => new(1, maxWidth);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => segments;
    }
}
