namespace WolfTodo.Tui.Features.Splash;

public sealed class HomeScreenReducer
{
    public HomeScreenTransition Reduce(HomeScreenState state, ConsoleKeyInfo key, Keybindings keybindings)
    {
        if (!state.IsCommandMode)
        {
            return key.KeyChar == ':'
                ? new HomeScreenTransition(new HomeScreenState(true, ":", null), false)
                : new HomeScreenTransition(state, false);
        }

        if (key.Key == ConsoleKey.Escape)
        {
            return new HomeScreenTransition(HomeScreenState.Initial, false);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            return state.Command == keybindings.QuitCommand
                ? new HomeScreenTransition(state, true)
                : new HomeScreenTransition(new HomeScreenState(false, string.Empty, $"Unknown command: {state.Command}"), false);
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            var command = state.Command.Length > 1 ? state.Command[..^1] : state.Command;
            return new HomeScreenTransition(new HomeScreenState(true, command, null), false);
        }

        return char.IsControl(key.KeyChar)
            ? new HomeScreenTransition(state, false)
            : new HomeScreenTransition(new HomeScreenState(true, state.Command + key.KeyChar, null), false);
    }
}
