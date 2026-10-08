using WolfTodo.Tui.Features.TodoEditing;
using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Features.TodoEditing;

public sealed class TodoTaskEditorDialogLineTests
{
    [Fact]
    public void Preserves_the_text_and_semantic_role()
    {
        var line = new TodoTaskEditorDialogLine("Invalid date", TodoTaskEditorDialogRole.Error);

        line.Text.Should().Be("Invalid date");
        line.Role.Should().Be(TodoTaskEditorDialogRole.Error);
    }
}
