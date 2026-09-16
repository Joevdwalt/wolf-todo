using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class NullApplicationFileChangeMonitorTests
{
    [Fact]
    public void Poll_never_reports_changes()
    {
        var monitor = NullApplicationFileChangeMonitor.Instance;
        monitor.WatchProjectFiles(["/todos/project.md"]);

        monitor.Poll().Should().Be(ApplicationFileChanges.None);
    }
}
