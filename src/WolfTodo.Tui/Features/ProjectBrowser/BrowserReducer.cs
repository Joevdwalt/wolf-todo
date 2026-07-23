using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class BrowserReducer
{
    private readonly TodoEditorReducer todoEditorReducer;
    private readonly Func<DateOnly> todayProvider;

    public BrowserReducer(Func<DateOnly>? todayProvider = null)
    {
        this.todayProvider = todayProvider ??
            (() => DateOnly.FromDateTime(DateTime.Today));
        todoEditorReducer = new TodoEditorReducer(this.todayProvider);
    }

    public BrowserTransition ReduceAction(
        BrowserState state,
        BrowserAction action,
        BrowserView view) => action switch
        {
            BrowserAction.Filter => Transition(state with
            {
                IsFilterMode = true,
                FilterDraft = state.FilterText,
                Error = null
            }),
            BrowserAction.Sort => Transition(state with { IsSortMode = true, Error = null }),
            BrowserAction.Create => Transition(state with
            {
                Editor = todoEditorReducer.CreateEditor(
                    view.Projects.FirstOrDefault(project => project.IsSelected)?.Project?.Path,
                    view.Projects.Any(project => project.Project is not null)),
                Error = null
            }),
            BrowserAction.Edit when view.SelectedTodo is not null && view.SelectedTodoIdentity is not null =>
                Transition(state with
                {
                    Editor = todoEditorReducer.EditEditor(view.SelectedTodo, view.SelectedTodoIdentity),
                    Error = null
                }),
            BrowserAction.EditExternal when view.SelectedTodoIdentity is not null =>
                ExternalEdit(state, view.SelectedTodoIdentity),
            BrowserAction.ToggleCompleted
                when view.SelectedTodoIdentity is not null => new BrowserTransition(
                    state with { Error = null },
                    BrowserOperation.ToggleCompleted,
                    view.SelectedTodoIdentity.ProjectPath,
                    view.SelectedTodoIdentity),
            BrowserAction.RollProjectToday => RollProjectToday(state, view),
            BrowserAction.ToggleDetails => ToggleDetails(state),
            BrowserAction.JumpTop => Jump(state, view, false),
            BrowserAction.JumpBottom => Jump(state, view, true),
            _ => Transition(state with { Error = "The selected action is not available." })
        };

    public BrowserTransition Reduce(
        BrowserState state,
        ConsoleKeyInfo key,
        ApplicationConfiguration configuration,
        BrowserView view)
    {
        var bindings = configuration.KeyBindings;

        if (state.Editor is not null)
        {
            return ApplyEditorTransition(
                state,
                todoEditorReducer.Reduce(
                    state.Editor,
                    key,
                    bindings,
                    EditorProjects(view)));
        }

        if (state.IsFilterMode)
        {
            return ReduceFilter(state, key);
        }

        if (state.IsSortMode)
        {
            return ReduceSort(state, key, view);
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

        if (bindings.MatchesCreateTodo(key))
        {
            var selectedProject = view.Projects.FirstOrDefault(project => project.IsSelected)?.Project;
            var projectPath = selectedProject?.Path;
            return Transition(state with
            {
                Editor = todoEditorReducer.CreateEditor(
                    projectPath,
                    view.Projects.Any(project => project.Project is not null)),
                Error = null
            });
        }

        if (bindings.MatchesEditTodoExternal(key))
        {
            if (view.SelectedTodoIdentity is null)
            {
                return Transition(state with { Error = "Select a todo to edit externally." });
            }

            return ExternalEdit(state, view.SelectedTodoIdentity);
        }

        if (bindings.MatchesEditTodo(key) || bindings.MatchesEditTodoContent(key))
        {
            if (view.SelectedTodo is null || view.SelectedTodoIdentity is null)
            {
                return Transition(state with { Error = "Select a todo to edit." });
            }

            return Transition(state with
            {
                Editor = todoEditorReducer.EditEditor(view.SelectedTodo, view.SelectedTodoIdentity),
                Error = null
            });
        }

        if (bindings.MatchesToggleTodo(key))
        {
            if (view.SelectedTodo is null || view.SelectedTodoIdentity is null)
            {
                return Transition(state with { Error = "Select a todo to complete." });
            }

            return new BrowserTransition(
                state with { Error = null },
                BrowserOperation.ToggleCompleted,
                view.SelectedTodoIdentity.ProjectPath,
                view.SelectedTodoIdentity);
        }

        if (bindings.MatchesRollProjectToday(key))
        {
            return RollProjectToday(state, view);
        }

        if (bindings.MatchesToggleDetails(key))
        {
            return ToggleDetails(state);
        }

        if (bindings.MatchesJumpTop(key) || bindings.MatchesJumpBottom(key))
        {
            return Jump(state, view, bindings.MatchesJumpBottom(key));
        }

        if (bindings.MatchesFocusNext(key) || bindings.MatchesFocusPrevious(key))
        {
            var reverse = bindings.MatchesFocusPrevious(key);
            return Transition(state with
            {
                Focus = MoveFocus(state.Focus, reverse, state.ShowDetails)
            });
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
                ShowDetails = state.Focus == BrowserFocus.Todos || state.ShowDetails,
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

    private static IReadOnlyList<TodoEditorProjectOption> EditorProjects(BrowserView view) =>
        view.Projects
            .Where(project => project.Project is not null)
            .Select(project => new TodoEditorProjectOption(project.Title, project.Project!.Path))
            .ToArray();

    private static BrowserTransition ApplyEditorTransition(
        BrowserState state,
        TodoEditorTransition transition) => new(
            state with
            {
                Editor = transition.Operation == TodoEditorOperation.None ? transition.State : state.Editor,
                Error = null
            },
            transition.Operation switch
            {
                TodoEditorOperation.Create => BrowserOperation.Create,
                TodoEditorOperation.Update => BrowserOperation.Update,
                _ => BrowserOperation.None
            },
            transition.ProjectPath,
            transition.Target,
            transition.Update);

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
            'd' => new TodoSort(TodoSortProperty.Schedule, TodoSortDirection.Ascending),
            'D' => new TodoSort(TodoSortProperty.Schedule, TodoSortDirection.Descending),
            't' => new TodoSort(TodoSortProperty.Tags, TodoSortDirection.Ascending),
            'T' => new TodoSort(TodoSortProperty.Tags, TodoSortDirection.Descending),
            'f' => new TodoSort(TodoSortProperty.File, TodoSortDirection.Ascending),
            'F' => new TodoSort(TodoSortProperty.File, TodoSortDirection.Descending),
            'p' => new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Ascending),
            'P' => new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Descending),
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

    private static BrowserTransition ToggleDetails(BrowserState state) => Transition(state with
    {
        ShowDetails = !state.ShowDetails,
        Focus = state.ShowDetails
            ? state.Focus == BrowserFocus.Details ? BrowserFocus.Todos : state.Focus
            : BrowserFocus.Details,
        Error = null
    });

    private BrowserTransition RollProjectToday(BrowserState state, BrowserView view)
    {
        var project = view.Projects.FirstOrDefault(row => row.IsSelected)?.Project;
        if (project is null)
        {
            return Transition(state with
            {
                Error = "Select a project before rolling tasks to today."
            });
        }

        var today = todayProvider();
        if (!Flatten(project.Todos).Any(todo =>
                !todo.IsCompleted && todo.Schedule?.Date < today))
        {
            return Transition(state with
            {
                Error = "The selected project has no incomplete overdue tasks."
            });
        }

        return new BrowserTransition(
            state with
            {
                PendingTodoSelection = view.SelectedTodoIdentity,
                Error = null
            },
            BrowserOperation.RollProjectToday,
            project.Path,
            view.SelectedTodoIdentity);
    }

    private static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var subtask in Flatten(todo.Subtasks))
            {
                yield return subtask;
            }
        }
    }

    private static BrowserTransition Jump(BrowserState state, BrowserView view, bool bottom) =>
        state.Focus == BrowserFocus.Projects
            ? Transition(state with
            {
                ProjectIndex = bottom ? Math.Max(0, view.Projects.Length - 1) : 0,
                TodoIndex = 0,
                PendingTodoSelection = null,
                Error = null
            })
            : Transition(state with
            {
                TodoIndex = bottom ? Math.Max(0, view.SelectableTodoCount - 1) : 0,
                PendingTodoSelection = null,
                Error = null
            });

    private static BrowserFocus MoveFocus(BrowserFocus focus, bool reverse, bool showDetails) =>
        !showDetails
            ? (focus, reverse) switch
            {
                (BrowserFocus.Projects, false) => BrowserFocus.Todos,
                (BrowserFocus.Todos, false) => BrowserFocus.Projects,
                (BrowserFocus.Projects, true) => BrowserFocus.Todos,
                _ => BrowserFocus.Projects
            }
            : (focus, reverse) switch
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

    private static BrowserTransition Transition(BrowserState state) => new(state);

    private static BrowserTransition ExternalEdit(BrowserState state, TodoIdentity identity) => new(
        state with { Error = null },
        BrowserOperation.EditExternal,
        identity.ProjectPath,
        identity);
}
