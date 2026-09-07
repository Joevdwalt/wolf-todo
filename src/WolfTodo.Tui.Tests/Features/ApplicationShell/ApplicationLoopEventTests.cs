using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class ApplicationLoopEventTests
{
    [Fact]
    public void Factory_methods_create_distinct_loop_events()
    {
        var key = new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false);

        ApplicationLoopEvent.ForKey(key).Key.Should().Be(key);
        ApplicationLoopEvent.ForFiles(new ApplicationFileChanges(false, true)).FileChanges.HasChanges.Should().BeTrue();
        ApplicationLoopEvent.ForRedraw().RedrawRequested.Should().BeTrue();
    }
}
