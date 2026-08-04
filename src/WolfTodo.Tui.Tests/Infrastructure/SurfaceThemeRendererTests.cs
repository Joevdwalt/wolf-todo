using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class SurfaceThemeRendererTests
{
    [Fact]
    public void Style_maps_foreground_background_and_decoration()
    {
        var renderer = new SurfaceThemeRenderer();

        var style = renderer.Style(Color.Red, Decoration.Bold, Color.Blue);

        style.Foreground.Should().Be(Color.Red);
        style.Background.Should().Be(Color.Blue);
        style.Decoration.Should().Be(Decoration.Bold);
    }

    [Fact]
    public void AppendStyled_escapes_markup_and_applies_requested_style()
    {
        var renderer = new SurfaceThemeRenderer();
        var output = new System.Text.StringBuilder();

        renderer.AppendStyled(output, "[value]", Color.Red, Decoration.Bold);

        output.ToString().Should().Contain("red").And.Contain("bold").And.Contain("[[value]]");
    }
}
