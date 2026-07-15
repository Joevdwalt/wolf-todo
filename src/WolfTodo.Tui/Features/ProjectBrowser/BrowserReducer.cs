using WolfTodo.Tui.Features.Configuration;

using System.Collections.Immutable;
using System.Globalization;
using WolfTodo.Core.Features.ProjectBrowser;

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

        if (state.Form is not null)
        {
            return ReduceForm(state, key, bindings, view);
        }

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

        if (bindings.MatchesCreateTodo(key))
        {
            var selectedProject = view.Projects.FirstOrDefault(project => project.IsSelected)?.Project;
            var projectPath = selectedProject?.Path;
            return Transition(state with
            {
                Form = NewForm(projectPath, view),
                Error = null
            });
        }

        if (bindings.MatchesEditTodo(key))
        {
            if (view.SelectedTodo is null || view.SelectedTodoIdentity is null)
            {
                return Transition(state with { Error = "Select a todo to edit." });
            }

            return Transition(state with
            {
                Form = EditForm(view.SelectedTodo, view.SelectedTodoIdentity),
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
                false,
                BrowserOperation.ToggleCompleted,
                view.SelectedTodoIdentity.ProjectPath,
                view.SelectedTodoIdentity);
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

    private static BrowserTransition ReduceForm(
        BrowserState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        BrowserView view)
    {
        var form = state.Form!;
        if (form.IsChoosingProject)
        {
            var projects = view.Projects.Where(project => project.Project is not null).ToArray();
            if (bindings.MatchesBack(key))
            {
                return Transition(state with { Form = null, Error = null });
            }

            if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
            {
                var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
                return Transition(state with
                {
                    Form = form with
                    {
                        ProjectPickerIndex = Math.Clamp(
                            form.ProjectPickerIndex + offset,
                            0,
                            Math.Max(0, projects.Length - 1)),
                        Error = null
                    }
                });
            }

            if (bindings.MatchesOpen(key) && projects.Length > 0)
            {
                return Transition(state with
                {
                    Form = form with
                    {
                        ProjectPath = projects[form.ProjectPickerIndex].Project!.Path,
                        Error = null
                    }
                });
            }

            return Transition(state);
        }

        if (form.IsEditing)
        {
            if (key.Key == ConsoleKey.Escape)
            {
                return Transition(state with { Form = form with { IsEditing = false, Error = null } });
            }

            if (key.Key == ConsoleKey.Enter)
            {
                return CommitDraft(state, form);
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                return Transition(state with
                {
                    Form = form with
                    {
                        Draft = form.Draft.Length == 0 ? string.Empty : form.Draft[..^1],
                        Error = null
                    }
                });
            }

            return char.IsControl(key.KeyChar)
                ? Transition(state)
                : Transition(state with
                {
                    Form = form with { Draft = form.Draft + key.KeyChar, Error = null }
                });
        }

        if (bindings.MatchesBack(key))
        {
            return Transition(state with { Form = null, Error = null });
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            var next = Math.Clamp((int)form.Field + offset, 0, Enum.GetValues<TodoFormField>().Length - 1);
            return Transition(state with { Form = form with { Field = (TodoFormField)next, Error = null } });
        }

        if (bindings.MatchesOpen(key))
        {
            return Transition(state with
            {
                Form = form with { IsEditing = true, Draft = FieldValue(form), Error = null }
            });
        }

        if (bindings.MatchesSaveForm(key))
        {
            if (string.IsNullOrWhiteSpace(form.Values.Title))
            {
                return Transition(state with { Form = form with { Error = "Title is required." } });
            }

            return new BrowserTransition(
                state with { Form = null, Error = null },
                false,
                form.IsCreate ? BrowserOperation.Create : BrowserOperation.Update,
                form.ProjectPath,
                form.Target,
                form.Values);
        }

        return Transition(state);
    }

    private static BrowserTransition CommitDraft(BrowserState state, TodoFormState form)
    {
        var values = form.Values;
        string? error = null;
        switch (form.Field)
        {
            case TodoFormField.Title:
                values = values with { Title = form.Draft.Trim() };
                if (values.Title.Length == 0)
                {
                    error = "Title is required.";
                }
                break;
            case TodoFormField.Reference:
                values = values with { ExternalReference = NullIfEmpty(form.Draft) };
                break;
            case TodoFormField.Priority:
                if (string.IsNullOrWhiteSpace(form.Draft))
                {
                    values = values with { Priority = null };
                }
                else if (Enum.TryParse<TodoPriority>(form.Draft, true, out var priority))
                {
                    values = values with { Priority = priority };
                }
                else
                {
                    error = "Priority must be Highest, High, Medium, Low, Lowest, or empty.";
                }
                break;
            case TodoFormField.Tags:
                values = values with
                {
                    Tags = form.Draft.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                        .Select(tag => tag.Trim().TrimStart('#'))
                        .Where(tag => tag.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToImmutableArray()
                };
                break;
            case TodoFormField.StartDate:
                values = values with { StartDate = ParseDate(form.Draft, out error) };
                break;
            case TodoFormField.DueDate:
                values = values with { DueDate = ParseDate(form.Draft, out error) };
                break;
        }

        return Transition(state with
        {
            Form = form with
            {
                Values = values,
                IsEditing = error is not null,
                Error = error
            }
        });
    }

    private static DateOnly? ParseDate(string value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        error = "Date must use YYYY-MM-DD or be empty.";
        return null;
    }

    private static TodoFormState NewForm(string? projectPath, BrowserView view) => new(
        true,
        projectPath,
        0,
        TodoFormField.Title,
        false,
        string.Empty,
        new TodoUpdate(string.Empty, null, null, [], null, null),
        null,
        view.Projects.Any(project => project.Project is not null) ? null : "No valid projects are available.");

    private static TodoFormState EditForm(TodoItem todo, TodoIdentity identity) => new(
        false,
        identity.ProjectPath,
        0,
        TodoFormField.Title,
        false,
        string.Empty,
        new TodoUpdate(
            todo.Title,
            todo.ExternalReference,
            todo.Priority,
            todo.Tags,
            todo.StartDate,
            todo.DueDate),
        identity,
        null);

    private static string FieldValue(TodoFormState form) => form.Field switch
    {
        TodoFormField.Title => form.Values.Title,
        TodoFormField.Reference => form.Values.ExternalReference ?? string.Empty,
        TodoFormField.Priority => form.Values.Priority?.ToString() ?? string.Empty,
        TodoFormField.Tags => string.Join(' ', form.Values.Tags.Select(tag => $"#{tag}")),
        TodoFormField.StartDate => form.Values.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        TodoFormField.DueDate => form.Values.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        _ => string.Empty
    };

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
