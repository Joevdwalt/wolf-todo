using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class TodoProjectReadResultTests
{
    [Fact]
    public void Factory_methods_expose_success_and_failure_states()
    {
        var project = new TodoProject("Work", "/todos/work.md", []);

        var success = TodoProjectReadResult.Success("/todos/work.md", project);
        var failure = TodoProjectReadResult.Failure("/todos/work.md", "Cannot read project file.");

        success.IsSuccess.Should().BeTrue();
        success.Project.Should().Be(project);
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("Cannot read project file.");
    }
}
