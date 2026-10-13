using FluentAssertions;
using Spectre.Console;
using Wolf.Controls.TextBox;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.Timing;
using WolfTodo.Tui.Features.Timing.Rendering;

namespace WolfTodo.Tui.Tests.Features.Timing.Rendering;

public sealed class PomodoroPromptRendererTests
{
    [Fact]
    public void Render_keeps_the_input_and_error_hint_with_the_expected_height()
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 30;
        var prompt = new PomodoroPromptState(
            TextBoxState.Create("POMODORO MINUTES", true, "25", true),
            null, null, null, "Enter a number");

        console.Write(PomodoroPromptRenderer.Render(prompt, TuiThemes.Wolf, 30));

        PomodoroPromptRenderer.Height.Should().Be(5);
        writer.ToString().Should().Contain("25").And.Contain("Enter a number")
            .And.Contain("Enter START").And.Contain("Esc CANCEL");
    }
}
