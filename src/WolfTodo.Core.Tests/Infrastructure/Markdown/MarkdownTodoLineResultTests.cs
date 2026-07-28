using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Infrastructure.Markdown;

public sealed class MarkdownTodoLineResultTests
{
    [Fact]
    public void Factory_methods_distinguish_non_tasks_successes_and_failures()
    {
        var line = new MarkdownTodoLine(
            0,
            new TodoItem(1, false, null, "Prepare workshop", null, [], null, null, string.Empty, [], []));

        MarkdownTodoLineResult.NotATask().IsTask.Should().BeFalse();
        MarkdownTodoLineResult.Success(line).Should().Be(new MarkdownTodoLineResult(line, null));
        MarkdownTodoLineResult.Failure("Invalid task.").Should().Be(new MarkdownTodoLineResult(null, "Invalid task."));
    }
}
