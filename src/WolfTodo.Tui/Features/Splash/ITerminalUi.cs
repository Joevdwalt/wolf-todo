using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.Splash;

public interface ITerminalUi
{
    void ShowSplash(string logo);

    void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings);

    void ShowStartupError(string message);

    void SetCursorVisible(bool visible);

    ConsoleKeyInfo ReadKey();
}
