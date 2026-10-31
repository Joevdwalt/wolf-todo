using WolfTodo.Tui.Features.TaskFocus;
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

    [Theory]
    [InlineData(100, 24)]
    [InlineData(80, 30)]
    public void Wait_redraws_after_dimensions_change_without_input(int width, int height)
    {
        var terminal = new StubTerminal { Dimensions = (80, 24) };
        terminal.OnRead = () => terminal.Dimensions = (width, height);
        var result = new ApplicationLoopWaiter(terminal, new StubMonitor(ApplicationFileChanges.None)).Wait(null);
        result.RedrawRequested.Should().BeTrue();
        terminal.ReadCount.Should().Be(1);
    }

    [Fact]
    public void Wait_ignores_unavailable_and_unchanged_dimensions_and_keeps_input()
    {
        var key = new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false);
        var terminal = new StubTerminal { Dimensions = (80, 24) };
        terminal.OnRead = () =>
        {
            if (terminal.ReadCount == 1) terminal.Dimensions = null;
            if (terminal.ReadCount == 2) terminal.Dimensions = (80, 24);
            if (terminal.ReadCount == 3) terminal.Key = key;
        };
        var result = new ApplicationLoopWaiter(terminal, new StubMonitor(ApplicationFileChanges.None)).Wait(null);
        result.Key.Should().Be(key);
        terminal.ReadCount.Should().Be(3);
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
        public (int Width, int Height)? Dimensions { get; set; }
        public Action? OnRead { get; set; }
        public ConsoleKeyInfo? Key { get; set; } = key;
        public (int Width, int Height)? GetDimensions() => Dimensions;
        public ConsoleKeyInfo? ReadKey(TimeSpan timeout) { ReadCount++; OnRead?.Invoke(); return Key; }
        public ConsoleKeyInfo ReadKey() => Key!.Value;
        public void ShowSplashAndWaitForDismissal(string logo, TuiTheme theme) { }
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
