using FluentAssertions;
using Spectre.Console;
using Wolf.Controls.TextBox;
using Wolf.Controls.TextPrompt;

namespace Wolf.Controls.Tests;

public sealed class TextPromptTests
{
    [Theory]
    [InlineData(null, 5)]
    [InlineData("Context", 6)]
    public void Measure_accounts_for_optional_context(string? context, int expected)
    {
        var view = new TextPromptView(TextBoxState.Create("Query", true, string.Empty), "Enter SEARCH", context);

        TextPrompt.TextPrompt.Measure(view).Should().Be(expected);
    }

    [Fact]
    public void Render_displays_context_input_and_error_footer_at_narrow_width()
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 12;

        console.Write(TextPrompt.TextPrompt.Render(
            new TextPromptView(TextBoxState.Create("A lengthy label", true, "abc"), "Invalid · Esc", "Context", IsError: true),
            ControlTheme.Default,
            new ControlConstraints(12, 6)));

        writer.ToString().Should().Contain("Context").And.Contain("abc").And.Contain("Invalid");
    }
}
