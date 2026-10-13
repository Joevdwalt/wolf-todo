using FluentAssertions;
using Spectre.Console;
using Wolf.Controls.FormDialog;
using Wolf.Controls.TextBox;

namespace Wolf.Controls.Tests;

public sealed class FormDialogTests
{
    [Fact]
    public void Measure_counts_lines_border_and_embedded_textboxes()
    {
        var view = new FormDialogView(
            [new FormDialogLine("Heading", FormDialogRole.Label), new FormDialogLine("Hint", FormDialogRole.Hint)],
            [TextBoxState.Create("Title", true, "Plan")],
            20);

        FormDialog.FormDialog.Measure(view, new ControlConstraints(20, 10)).Should().Be(8);
    }

    [Theory]
    [InlineData(FormDialogRole.Label, "Heading")]
    [InlineData(FormDialogRole.Value, "Value")]
    [InlineData(FormDialogRole.ActiveValue, "Active")]
    [InlineData(FormDialogRole.Placeholder, "Placeholder")]
    [InlineData(FormDialogRole.Hint, "Hint")]
    [InlineData(FormDialogRole.Error, "Error")]
    [InlineData(FormDialogRole.Warning, "Warning")]
    public void Render_shows_each_semantic_line(FormDialogRole role, string value)
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 60;

        console.Write(FormDialog.FormDialog.Render(
            new FormDialogView([new FormDialogLine(value, role)]), ControlTheme.Default));

        writer.ToString().Should().Contain(value).And.Contain("┌").And.Contain("┘");
    }
}
