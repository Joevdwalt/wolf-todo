using WolfTodo.Tui.Features.Configuration;

using System.Collections.Immutable;
using System.Globalization;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class BrowserReducer
{
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
                Form = NewForm(
                    view.Projects.FirstOrDefault(project => project.IsSelected)?.Project?.Path,
                    view),
                Error = null
            }),
            BrowserAction.Edit when view.SelectedTodo is not null && view.SelectedTodoIdentity is not null =>
                Transition(state with
                {
                    Form = EditForm(view.SelectedTodo, view.SelectedTodoIdentity),
                    Error = null
                }),
            BrowserAction.EditContent
                when view.SelectedTodo is not null && view.SelectedTodoIdentity is not null =>
                Transition(state with
                {
                    ContentEditor = TodoContentEditorState.Create(
                        view.SelectedTodoIdentity,
                        view.SelectedTodo),
                    Error = null
                }),
            BrowserAction.ToggleCompleted
                when view.SelectedTodoIdentity is not null => new BrowserTransition(
                    state with { Error = null },
                    BrowserOperation.ToggleCompleted,
                    view.SelectedTodoIdentity.ProjectPath,
                    view.SelectedTodoIdentity),
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

        if (state.ContentEditor is not null)
        {
            return ReduceContentEditor(state, key, bindings);
        }

        if (state.Form is not null)
        {
            return ReduceForm(state, key, bindings, view);
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

        if (bindings.MatchesEditTodoContent(key))
        {
            if (view.SelectedTodo is null || view.SelectedTodoIdentity is null)
            {
                return Transition(state with { Error = "Select a todo to edit its content." });
            }

            return Transition(state with
            {
                ContentEditor = TodoContentEditorState.Create(
                    view.SelectedTodoIdentity,
                    view.SelectedTodo),
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

    private static BrowserTransition ReduceContentEditor(
        BrowserState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        var editor = state.ContentEditor!;
        if (editor.Mode == ContentEditorMode.Edit)
        {
            if (key.Key == ConsoleKey.Escape)
            {
                return Transition(state with
                {
                    ContentEditor = editor with
                    {
                        Mode = ContentEditorMode.Browse,
                        IsAdding = false,
                        Draft = string.Empty,
                        Error = null
                    }
                });
            }

            if (key.Key == ConsoleKey.Enter)
            {
                return CommitContentDraft(state, editor);
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                return Transition(state with
                {
                    ContentEditor = editor with
                    {
                        Draft = editor.Draft.Length == 0 ? string.Empty : editor.Draft[..^1],
                        Error = null
                    }
                });
            }

            return char.IsControl(key.KeyChar)
                ? Transition(state)
                : Transition(state with
                {
                    ContentEditor = editor with { Draft = editor.Draft + key.KeyChar, Error = null }
                });
        }

        if (editor.Mode == ContentEditorMode.ConfirmRemoval)
        {
            if (bindings.MatchesOpen(key))
            {
                return Transition(state with
                {
                    ContentEditor = RemoveSelectedSubtask(editor) with
                    {
                        Mode = ContentEditorMode.Browse,
                        Error = null
                    }
                });
            }

            return bindings.MatchesBack(key)
                ? Transition(state with
                {
                    ContentEditor = editor with { Mode = ContentEditorMode.Browse, Error = null }
                })
                : Transition(state);
        }

        if (bindings.MatchesBack(key))
        {
            return Transition(state with { ContentEditor = null, Error = null });
        }

        if (bindings.MatchesFocusNext(key) || bindings.MatchesFocusPrevious(key))
        {
            var focus = editor.Focus == ContentEditorFocus.Notes
                ? ContentEditorFocus.Subtasks
                : ContentEditorFocus.Notes;
            return Transition(state with { ContentEditor = editor with { Focus = focus, Error = null } });
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            var updated = editor.Focus == ContentEditorFocus.Notes
                ? editor with
                {
                    NoteIndex = Math.Clamp(
                        editor.NoteIndex + offset,
                        0,
                        Math.Max(0, editor.Notes.Length - 1)),
                    Error = null
                }
                : editor with
                {
                    SubtaskIndex = Math.Clamp(
                        editor.SubtaskIndex + offset,
                        0,
                        Math.Max(0, editor.Subtasks.Length - 1)),
                    Error = null
                };
            return Transition(state with { ContentEditor = updated });
        }

        if (bindings.MatchesCreateTodo(key))
        {
            return Transition(state with
            {
                ContentEditor = editor with
                {
                    Mode = ContentEditorMode.Edit,
                    IsAdding = true,
                    Draft = string.Empty,
                    Error = null
                }
            });
        }

        if (bindings.MatchesEditTodo(key) || bindings.MatchesOpen(key))
        {
            var value = editor.Focus == ContentEditorFocus.Notes
                ? editor.Notes.ElementAtOrDefault(editor.NoteIndex)?.Text
                : editor.Subtasks.ElementAtOrDefault(editor.SubtaskIndex)?.Title;
            return value is null
                ? Transition(state with
                {
                    ContentEditor = editor with { Error = "There is no content to edit." }
                })
                : Transition(state with
                {
                    ContentEditor = editor with
                    {
                        Mode = ContentEditorMode.Edit,
                        IsAdding = false,
                        Draft = value,
                        Error = null
                    }
                });
        }

        if (bindings.MatchesRemoveContent(key))
        {
            if (editor.Focus == ContentEditorFocus.Notes)
            {
                if (editor.Notes.Length == 0)
                {
                    return Transition(state with
                    {
                        ContentEditor = editor with { Error = "There is no note to remove." }
                    });
                }

                var notes = editor.Notes.RemoveAt(editor.NoteIndex);
                return Transition(state with
                {
                    ContentEditor = editor with
                    {
                        Notes = notes,
                        NoteIndex = Math.Clamp(editor.NoteIndex, 0, Math.Max(0, notes.Length - 1)),
                        Error = null
                    }
                });
            }

            if (editor.Subtasks.Length == 0)
            {
                return Transition(state with
                {
                    ContentEditor = editor with { Error = "There is no subtask to remove." }
                });
            }

            var selected = editor.Subtasks[editor.SubtaskIndex];
            return selected.DescendantCount > 0
                ? Transition(state with
                {
                    ContentEditor = editor with { Mode = ContentEditorMode.ConfirmRemoval, Error = null }
                })
                : Transition(state with { ContentEditor = RemoveSelectedSubtask(editor) });
        }

        if (editor.Focus == ContentEditorFocus.Subtasks && bindings.MatchesToggleTodo(key))
        {
            if (editor.Subtasks.Length == 0)
            {
                return Transition(state with
                {
                    ContentEditor = editor with { Error = "There is no subtask to toggle." }
                });
            }

            var selected = editor.Subtasks[editor.SubtaskIndex];
            return Transition(state with
            {
                ContentEditor = editor with
                {
                    Subtasks = editor.Subtasks.SetItem(
                        editor.SubtaskIndex,
                        selected with { IsCompleted = !selected.IsCompleted }),
                    Error = null
                }
            });
        }

        if (bindings.MatchesSaveForm(key))
        {
            return new BrowserTransition(
                state with { ContentEditor = null, Error = null },
                BrowserOperation.UpdateContent,
                editor.Target.ProjectPath,
                editor.Target,
                ContentUpdate: editor.ToUpdate());
        }

        return Transition(state);
    }

    private static BrowserTransition CommitContentDraft(
        BrowserState state,
        TodoContentEditorState editor)
    {
        var value = editor.Draft.Trim();
        if (value.Length == 0)
        {
            return Transition(state with
            {
                ContentEditor = editor with { Error = "Content must not be empty." }
            });
        }

        if (editor.Focus == ContentEditorFocus.Notes)
        {
            var notes = editor.IsAdding
                ? editor.Notes.Add(new ContentNoteDraft(null, value))
                : editor.Notes.SetItem(editor.NoteIndex, editor.Notes[editor.NoteIndex] with { Text = value });
            editor = editor with { Notes = notes, NoteIndex = notes.Length - 1 };
        }
        else
        {
            var subtasks = editor.IsAdding
                ? editor.Subtasks.Add(new ContentSubtaskDraft(null, value, false, 0))
                : editor.Subtasks.SetItem(
                    editor.SubtaskIndex,
                    editor.Subtasks[editor.SubtaskIndex] with { Title = value });
            editor = editor with { Subtasks = subtasks, SubtaskIndex = subtasks.Length - 1 };
        }

        return Transition(state with
        {
            ContentEditor = editor with
            {
                Mode = ContentEditorMode.Browse,
                IsAdding = false,
                Draft = string.Empty,
                Error = null
            }
        });
    }

    private static TodoContentEditorState RemoveSelectedSubtask(TodoContentEditorState editor)
    {
        var subtasks = editor.Subtasks.RemoveAt(editor.SubtaskIndex);
        return editor with
        {
            Subtasks = subtasks,
            SubtaskIndex = Math.Clamp(editor.SubtaskIndex, 0, Math.Max(0, subtasks.Length - 1)),
            Error = null
        };
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

    private static BrowserTransition ToggleDetails(BrowserState state) => Transition(state with
    {
        ShowDetails = !state.ShowDetails,
        Focus = state.ShowDetails
            ? state.Focus == BrowserFocus.Details ? BrowserFocus.Todos : state.Focus
            : BrowserFocus.Details,
        Error = null
    });

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
}
