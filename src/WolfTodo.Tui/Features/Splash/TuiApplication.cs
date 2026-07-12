namespace WolfTodo.Tui.Features.Splash;

public sealed class TuiApplication(
    IKeybindingsLoader keybindingsLoader,
    ITerminalUi terminalUi,
    HomeScreenReducer homeScreenReducer,
    string logo)
{
    public int Run()
    {
        Keybindings keybindings;

        try
        {
            keybindings = keybindingsLoader.Load();
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            terminalUi.ShowStartupError(exception.Message);
            return 1;
        }

        terminalUi.ShowSplash(logo);
        terminalUi.ReadKey();

        var state = HomeScreenState.Initial;

        while (true)
        {
            terminalUi.ShowHome(state);
            var transition = homeScreenReducer.Reduce(state, terminalUi.ReadKey(), keybindings);

            if (transition.ShouldExit)
            {
                return 0;
            }

            state = transition.State;
        }
    }
}
