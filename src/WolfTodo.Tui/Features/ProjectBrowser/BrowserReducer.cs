using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class BrowserReducer
{
    public BrowserTransition Reduce(
        BrowserState state,
        ConsoleKeyInfo key,
        ApplicationConfiguration configuration,
        BrowserView view)
    {
        var bindings = configuration.KeyBindings;

        if (state.IsCommandMode)
        {
            return ReduceCommand(state, key, bindings);
        }

        if (state.IsFilterMode)
        {
            return ReduceFilter(state, key);
        }

        if (state.IsSortMode)
        {
            return ReduceSort(state, key, view);
        }

        if (bindings.MatchesCommandMode(key))
        {
            return Transition(state with
            {
                IsCommandMode = true,
                Command = ":",
                Error = null
            });
        }

        if (bindings.MatchesFilterMode(key))
        {
            return Transition(state with
            {
                IsFilterMode = true,
                FilterDraft = state.FilterText,
                Error = null
            });
        }

        if (bindings.MatchesSortMode(key))
        {
            return Transition(state with
            {
                IsSortMode = true,
                Error = null
            });
        }

        if (bindings.MatchesFocusNext(key) || bindings.MatchesFocusPrevious(key))
        {
            var reverse = bindings.MatchesFocusPrevious(key);
            return Transition(state with { Focus = MoveFocus(state.Focus, reverse) });
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;

            return state.Focus == BrowserFocus.Projects
                ? Transition(state with
                {
                    ProjectIndex = MoveIndex(state.ProjectIndex, offset, view.Projects.Length),
                    TodoIndex = 0,
                    PendingTodoSelection = null,
                    Error = null
                })
                : Transition(state with
                {
                    TodoIndex = MoveIndex(state.TodoIndex, offset, view.SelectableTodoCount),
                    PendingTodoSelection = null,
                    Error = null
                });
        }

        if (bindings.MatchesOpen(key))
        {
            return Transition(state with
            {
                Focus = state.Focus switch
                {
                    BrowserFocus.Projects => BrowserFocus.Todos,
                    BrowserFocus.Todos => BrowserFocus.Details,
                    _ => BrowserFocus.Details
                },
                Error = null
            });
        }

        if (bindings.MatchesBack(key))
        {
            return Transition(state with
            {
                Focus = state.Focus == BrowserFocus.Details ? BrowserFocus.Todos : BrowserFocus.Projects,
                Error = null
            });
        }

        return Transition(state);
    }

    private static BrowserTransition ReduceSort(BrowserState state, ConsoleKeyInfo key, BrowserView view)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return Transition(state with { IsSortMode = false, Error = null });
        }

        var sort = key.KeyChar switch
        {
            'n' => new TodoSort(TodoSortProperty.Name, TodoSortDirection.Ascending),
            'N' => new TodoSort(TodoSortProperty.Name, TodoSortDirection.Descending),
            'd' => new TodoSort(TodoSortProperty.StartDate, TodoSortDirection.Ascending),
            'D' => new TodoSort(TodoSortProperty.StartDate, TodoSortDirection.Descending),
            't' => new TodoSort(TodoSortProperty.Tags, TodoSortDirection.Ascending),
            'T' => new TodoSort(TodoSortProperty.Tags, TodoSortDirection.Descending),
            'f' => new TodoSort(TodoSortProperty.File, TodoSortDirection.Ascending),
            'F' => new TodoSort(TodoSortProperty.File, TodoSortDirection.Descending),
            'o' => TodoSort.Source,
            _ => null
        };

        return sort is null
            ? Transition(state)
            : Transition(state with
            {
                IsSortMode = false,
                Sort = sort,
                PendingTodoSelection = view.SelectedTodoIdentity,
                Error = null
            });
    }

    private static BrowserTransition ReduceFilter(BrowserState state, ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return Transition(state with
            {
                IsFilterMode = false,
                FilterDraft = state.FilterText,
                PendingTodoSelection = null,
                Error = null
            });
        }

        if (key.Key == ConsoleKey.Enter)
        {
            var filter = state.FilterDraft.Trim();
            return Transition(state with
            {
                IsFilterMode = false,
                FilterText = filter,
                FilterDraft = filter,
                TodoIndex = 0,
                PendingTodoSelection = null,
                Error = null
            });
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            var filter = state.FilterDraft.Length > 0 ? state.FilterDraft[..^1] : state.FilterDraft;
            return Transition(state with
            {
                FilterDraft = filter,
                TodoIndex = 0,
                PendingTodoSelection = null,
                Error = null
            });
        }

        return char.IsControl(key.KeyChar)
            ? Transition(state)
            : Transition(state with
            {
                FilterDraft = state.FilterDraft + key.KeyChar,
                TodoIndex = 0,
                PendingTodoSelection = null,
                Error = null
            });
    }

    private static BrowserTransition ReduceCommand(
        BrowserState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return Transition(state with
            {
                IsCommandMode = false,
                Command = string.Empty,
                Error = null
            });
        }

        if (key.Key == ConsoleKey.Enter)
        {
            if (state.Command == bindings.QuitCommand)
            {
                return new BrowserTransition(state, true);
            }

            if (state.Command == bindings.ToggleCompletedCommand)
            {
                return Transition(state with
                {
                    ShowCompleted = !state.ShowCompleted,
                    IsCommandMode = false,
                    Command = string.Empty,
                    TodoIndex = 0,
                    Error = null
                });
            }

            return Transition(state with
            {
                IsCommandMode = false,
                Command = string.Empty,
                Error = $"Unknown command: {state.Command}"
            });
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            var command = state.Command.Length > 1 ? state.Command[..^1] : state.Command;
            return Transition(state with { Command = command, Error = null });
        }

        return char.IsControl(key.KeyChar)
            ? Transition(state)
            : Transition(state with { Command = state.Command + key.KeyChar, Error = null });
    }

    private static BrowserFocus MoveFocus(BrowserFocus focus, bool reverse) => (focus, reverse) switch
    {
        (BrowserFocus.Projects, false) => BrowserFocus.Todos,
        (BrowserFocus.Todos, false) => BrowserFocus.Details,
        (BrowserFocus.Details, false) => BrowserFocus.Projects,
        (BrowserFocus.Projects, true) => BrowserFocus.Details,
        (BrowserFocus.Todos, true) => BrowserFocus.Projects,
        _ => BrowserFocus.Todos
    };

    private static int MoveIndex(int current, int offset, int count) =>
        count == 0 ? 0 : Math.Clamp(current + offset, 0, count - 1);

    private static BrowserTransition Transition(BrowserState state) => new(state, false);
}
