using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Tests.Rendering;

public sealed class HeightConstrainedRenderableTests
{
    [Fact]
    public void Render_wraps_to_physical_rows_and_marks_clipped_content()
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 10;

        console.Write(new HeightConstrainedRenderable(
            new Text("alpha beta gamma\ndelta epsilon"),
            2));

        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().Be("alpha beta");
        lines[1].Should().EndWith("…");
    }

    [Fact]
    public void Render_pads_short_content_to_the_requested_height()
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 10;

        console.Write(new HeightConstrainedRenderable(new Text("alpha"), 3));

        writer.ToString().Split(Environment.NewLine).Should().HaveCount(3);
    }

    [Fact]
    public void Render_reserves_display_cell_width_for_the_overflow_marker()
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 4;

        console.Write(new HeightConstrainedRenderable(new Text("狼狼狼\nnext"), 1));

        writer.ToString().Should().EndWith("…").And.NotContain("狼狼狼");
    }
}
