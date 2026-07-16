using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.Splash;

public interface ITerminalUi
{
    void ShowSplash(string logo, TuiTheme theme);

    void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings, TuiTheme theme);

    void ShowPlanner(TabStripView tabs, PlannerView view, TuiKeyBindings keyBindings, TuiTheme theme);

    void ShowStartupError(string message);

    void SetCursorVisible(bool visible);

    void SuspendForExternalProcess();

    void ResumeAfterExternalProcess();

    ConsoleKeyInfo ReadKey();
}
