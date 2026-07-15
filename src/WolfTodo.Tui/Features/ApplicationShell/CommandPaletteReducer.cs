using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class CommandPaletteReducer
{
    public CommandPaletteTransition Reduce(
        CommandPaletteState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        CommandPaletteView view)
    {
        if (!state.IsOpen)
        {
            return bindings.MatchesCommandPalette(key)
                ? new CommandPaletteTransition(state with
                {
                    IsOpen = true,
                    SelectedIndex = 0,
                    Error = null
                })
                : new CommandPaletteTransition(state);
        }

        if (key.Key == ConsoleKey.Escape || (!state.IsSearching && bindings.MatchesBack(key)))
        {
            return state.IsSearching
                ? new CommandPaletteTransition(state with
                {
                    IsSearching = false,
                    Query = string.Empty,
                    SelectedIndex = 0,
                    Error = null
                })
                : new CommandPaletteTransition(CommandPaletteState.Closed);
        }

        if (!state.IsSearching && bindings.MatchesFilterMode(key))
        {
            return new CommandPaletteTransition(state with { IsSearching = true, Error = null });
        }

        var moveUp = state.IsSearching
            ? MatchesNonPrintable(bindings.MoveUp, key)
            : bindings.MatchesMoveUp(key);
        var moveDown = state.IsSearching
            ? MatchesNonPrintable(bindings.MoveDown, key)
            : bindings.MatchesMoveDown(key);
        if (moveUp || moveDown)
        {
            var offset = moveUp ? -1 : 1;
            return new CommandPaletteTransition(state with
            {
                SelectedIndex = Math.Clamp(
                    view.SelectedIndex + offset,
                    0,
                    Math.Max(0, view.Items.Length - 1)),
                Error = null
            });
        }

        if (bindings.MatchesOpen(key))
        {
            var selected = view.SelectedItem;
            if (selected is null)
            {
                return new CommandPaletteTransition(state with { Error = "No matching actions." });
            }

            return selected.IsEnabled
                ? new CommandPaletteTransition(CommandPaletteState.Closed, selected.Action)
                : new CommandPaletteTransition(state with { Error = selected.DisabledReason });
        }

        if (!state.IsSearching)
        {
            return new CommandPaletteTransition(state);
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            return new CommandPaletteTransition(state with
            {
                Query = state.Query.Length == 0 ? string.Empty : state.Query[..^1],
                SelectedIndex = 0,
                Error = null
            });
        }

        return char.IsControl(key.KeyChar)
            ? new CommandPaletteTransition(state)
            : new CommandPaletteTransition(state with
            {
                Query = state.Query + key.KeyChar,
                SelectedIndex = 0,
                Error = null
            });
    }

    private static bool MatchesNonPrintable(
        System.Collections.Immutable.ImmutableArray<KeyGesture> gestures,
        ConsoleKeyInfo key) => gestures.Any(gesture => gesture.Character is null && gesture.Matches(key));
}
