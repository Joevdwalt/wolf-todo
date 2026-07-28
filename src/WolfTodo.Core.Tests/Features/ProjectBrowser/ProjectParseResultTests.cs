using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class ProjectParseResultTests
{
    [Fact]
    public void Factory_methods_expose_success_and_failure_states()
    {
        var project = new TodoProject("Work", "/todos/work.md", []);

        var success = ProjectParseResult.Success(project);
        var failure = ProjectParseResult.Failure("Invalid markdown.");

        success.IsSuccess.Should().BeTrue();
        success.Project.Should().Be(project);
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("Invalid markdown.");
    }
}
