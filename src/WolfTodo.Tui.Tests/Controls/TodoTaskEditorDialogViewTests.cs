using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TodoTaskEditorDialogViewTests
{
    [Fact]
    public void Height_includes_logical_lines_dialog_chrome_and_embedded_textboxes()
    {
        var view = new TodoTaskEditorDialogView(
            [new TodoTaskEditorDialogLine("Heading", TodoTaskEditorDialogRole.Label)],
            [TextBox.Create("Title", false, "Plan")],
            TextBoxWidth: 20);

        view.Height.Should().Be(7);
    }
}
