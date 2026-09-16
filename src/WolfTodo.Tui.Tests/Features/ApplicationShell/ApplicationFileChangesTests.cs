using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class ApplicationFileChangesTests
{
    [Fact]
    public void Merge_combines_configuration_and_project_changes()
    {
        var result = new ApplicationFileChanges(true, false)
            .Merge(new ApplicationFileChanges(false, true));

        result.Should().Be(new ApplicationFileChanges(true, true));
        result.HasChanges.Should().BeTrue();
    }
}
