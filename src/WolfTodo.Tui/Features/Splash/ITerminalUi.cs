namespace WolfTodo.Tui.Features.Splash;

public interface ITerminalUi
{
    void ShowSplash(string logo);

    void ShowHome(HomeScreenState state);

    void ShowStartupError(string message);

    ConsoleKeyInfo ReadKey();
}
