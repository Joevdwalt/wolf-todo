using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.TaskFocus;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

internal sealed class BrowserWorkflowTestTerminal : ITerminalUi
{
    public int Suspensions { get; private set; }
    public int Resumptions { get; private set; }
    public void SuspendForExternalProcess() => Suspensions++;
    public void ResumeAfterExternalProcess() => Resumptions++;
    public void ShowSplashAndWaitForDismissal(string logo, TuiTheme theme) { }
    public void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings, TuiTheme theme) { }
    public void ShowPlanner(TabStripView tabs, PlannerView view, TuiKeyBindings keyBindings, TuiTheme theme) { }
    public void ShowFocusedTask(FocusedTaskView view, TuiKeyBindings keyBindings, TuiTheme theme) { }
    public void ShowStartupError(string message) { }
    public void SetCursorVisible(bool visible) { }
    public void RingBell() { }
    public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
    public ConsoleKeyInfo? ReadKey(TimeSpan timeout) => throw new NotSupportedException();
}
