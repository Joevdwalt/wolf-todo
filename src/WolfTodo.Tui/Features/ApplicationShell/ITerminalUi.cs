using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public interface ITerminalUi
{
    void ShowSplash(string logo, TuiTheme theme);

    void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings, TuiTheme theme);

    void ShowPlanner(TabStripView tabs, PlannerView view, TuiKeyBindings keyBindings, TuiTheme theme);

    void ShowStartupError(string message);

    void SetCursorVisible(bool visible);

    void SuspendForExternalProcess();

    void ResumeAfterExternalProcess();

    void RingBell();

    ScreenDumpResult DumpScreen() => new(null, "Screen dumping is unavailable.");

    ConsoleKeyInfo ReadKey();

    ConsoleKeyInfo? ReadKey(TimeSpan timeout);
}
