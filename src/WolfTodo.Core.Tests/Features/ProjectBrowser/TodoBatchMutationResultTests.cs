using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class TodoBatchMutationResultTests
{
    [Fact]
    public void Success_captures_source_lines()
    {
        var result = TodoBatchMutationResult.Success([4, 7]);

        result.Succeeded.Should().BeTrue();
        result.SourceLines.Should().Equal(4, 7);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_captures_error()
    {
        var result = TodoBatchMutationResult.Failure("invalid");

        result.Succeeded.Should().BeFalse();
        result.SourceLines.Should().BeEmpty();
        result.Error.Should().Be("invalid");
    }
}
