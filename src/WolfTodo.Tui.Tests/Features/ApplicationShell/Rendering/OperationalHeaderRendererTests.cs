using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell.Rendering;

public sealed class OperationalHeaderRendererTests
{
    private static readonly IAnsiConsole BaseConsole = AnsiConsole.Console;

    [Fact]
    public void Write_keeps_the_live_time_before_lower_priority_header_fields()
    {
        var recording = BaseConsole.CreateRecorder();
        AnsiConsole.Console = recording;
        AnsiConsole.Profile.Width = 40;
        AnsiConsole.Profile.Height = 16;

        var tabs = new TabStripView(
            [new TabItemView(new TabId("todos"), "Todos", true)]);
        var bindings = TuiKeyBindings.CreateDefaults(":q");

        new OperationalHeaderRenderer().Write(
            tabs,
            bindings,
            TuiThemes.Wolf,
            40,
            "BROWSE",
            new DateOnly(2026, 8, 4),
            new DateTime(2026, 8, 4, 14, 23, 0),
            3,
            0);

        var header = recording.ExportText().Split(Environment.NewLine).First();
        header.Should().Contain("TIME:14:23");
        header.IndexOf("TIME:14:23", StringComparison.Ordinal)
            .Should().BeLessThan(header.IndexOf("MODE:BROWSE", StringComparison.Ordinal));
        header.GetCellWidth().Should().BeLessThanOrEqualTo(40);
    }
}
