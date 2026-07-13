using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.Splash;

public interface ITerminalUi
{
    void ShowSplash(string logo);

    void ShowBrowser(BrowserView view, BrowserKeyBindings keyBindings);

    void ShowStartupError(string message);

    ConsoleKeyInfo ReadKey();
}
