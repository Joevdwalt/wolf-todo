using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Infrastructure.Markdown;

public sealed class MarkdownTodoItemsResultTests
{
    [Fact]
    public void Factory_methods_expose_success_and_failure_states()
    {
        IReadOnlyList<TodoItem> todos = [new TodoItem(1, false, null, "Prepare workshop", null, [], null, null, string.Empty, [], [])];

        var success = MarkdownTodoItemsResult.Success(todos);
        var failure = MarkdownTodoItemsResult.Failure("Invalid Markdown.");

        success.IsSuccess.Should().BeTrue();
        success.Todos.Should().BeSameAs(todos);
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("Invalid Markdown.");
    }
}
