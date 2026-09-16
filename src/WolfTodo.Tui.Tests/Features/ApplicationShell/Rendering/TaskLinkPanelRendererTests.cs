using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell.Rendering;

public sealed class TaskLinkPanelRendererTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_displays_the_context_input_and_matching_action_hint(bool opening)
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 120;
        var panel = new TaskLinkPanelState(TextBox.Create("TASK LINK", true, "wt1-example", true), opening, "Work · Line 3");
        console.Write(TaskLinkPanelRenderer.Render(panel, TuiThemes.Wolf, 120));
        writer.ToString().Should().Contain("Work · Line 3").And.Contain("wt1-example")
            .And.Contain(opening ? "Enter OPEN" : "Ctrl+A SELECT");
    }
}
