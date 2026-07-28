using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Infrastructure.Markdown;

public sealed class MarkdownTodoLineTests
{
    [Fact]
    public void Preserves_the_indentation_and_parsed_todo()
    {
        var todo = new TodoItem(3, false, null, "Prepare workshop", null, [], null, null, string.Empty, [], []);
        var line = new MarkdownTodoLine(2, todo);

        line.Indent.Should().Be(2);
        line.Todo.Should().Be(todo);
    }
}
