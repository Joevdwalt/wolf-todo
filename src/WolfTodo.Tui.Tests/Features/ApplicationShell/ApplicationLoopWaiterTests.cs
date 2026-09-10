using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class ApplicationLoopWaiterTests
{
    [Fact]
    public void Wait_returns_a_pending_file_change_without_reading_a_key()
    {
        var terminal = new StubTerminal();
        var monitor = new StubMonitor(new ApplicationFileChanges(false, true));

        var result = new ApplicationLoopWaiter(terminal, monitor).Wait(null);

        result.FileChanges.ProjectFilesChanged.Should().BeTrue();
        terminal.ReadCount.Should().Be(0);
    }

    [Fact]
    public void Wait_returns_an_available_key()
    {
        var expected = new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false);
        var terminal = new StubTerminal(expected);

        var result = new ApplicationLoopWaiter(terminal, new StubMonitor(ApplicationFileChanges.None)).Wait(null);

        result.Key.Should().Be(expected);
    }

    private sealed class StubMonitor(ApplicationFileChanges changes) : IApplicationFileChangeMonitor
    {
        public void WatchProjectFiles(IEnumerable<string> paths) { }
        public ApplicationFileChanges Poll() => changes;
        public void Dispose() { }
    }

    private sealed class StubTerminal(ConsoleKeyInfo? key = null) : ITerminalUi
    {
        public int ReadCount { get; private set; }
        public ConsoleKeyInfo? ReadKey(TimeSpan timeout) { ReadCount++; return key; }
        public ConsoleKeyInfo ReadKey() => key!.Value;
        public void ShowSplash(string logo, TuiTheme theme) { }
        public void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings, TuiTheme theme) { }
        public void ShowPlanner(TabStripView tabs, PlannerView view, TuiKeyBindings keyBindings, TuiTheme theme) { }
        public void ShowFocusedTask(FocusedTaskView view, TuiKeyBindings keyBindings, TuiTheme theme) { }
        public void ShowStartupError(string message) { }
        public void SetCursorVisible(bool visible) { }
        public void SuspendForExternalProcess() { }
        public void ResumeAfterExternalProcess() { }
        public void RingBell() { }
    }
}
