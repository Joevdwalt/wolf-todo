using System.Collections.Immutable;
using System.Globalization;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoEditorProjectOption(string Title, string Path);

public enum TodoEditorOperation
{
    None,
    Create,
    Update,
    UpdateContent
}

public sealed record TodoFormTransition(
    TodoFormState? State,
    TodoEditorOperation Operation = TodoEditorOperation.None,
    string? ProjectPath = null,
    TodoIdentity? Target = null,
    TodoUpdate? Update = null);

public sealed record TodoContentEditorTransition(
    TodoContentEditorState? State,
    TodoEditorOperation Operation = TodoEditorOperation.None,
    TodoIdentity? Target = null,
    TodoContentUpdate? Update = null);

public sealed class TodoEditorReducer
{
    public TodoFormState CreateForm(string? projectPath, bool hasProjects) => new(
        true,
        projectPath,
        0,
        TodoFormField.Title,
        false,
        string.Empty,
        new TodoUpdate(string.Empty, null, null, [], null, null),
        null,
        hasProjects ? null : "No valid projects are available.");

    public TodoFormState EditForm(TodoItem todo, TodoIdentity identity) => new(
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

    public TodoFormTransition ReduceForm(
        TodoFormState form,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        IReadOnlyList<TodoEditorProjectOption> projects)
    {
        if (form.IsChoosingProject)
        {
            if (bindings.MatchesBack(key))
            {
                return new TodoFormTransition(null);
            }

            if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
            {
                var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
                return Transition(form with
                {
                    ProjectPickerIndex = Math.Clamp(
                        form.ProjectPickerIndex + offset,
                        0,
                        Math.Max(0, projects.Count - 1)),
                    Error = null
                });
            }

            if (bindings.MatchesOpen(key) && projects.Count > 0)
            {
                return Transition(form with
                {
                    ProjectPath = projects[form.ProjectPickerIndex].Path,
                    Error = null
                });
            }

            return Transition(form);
        }

        if (form.IsEditing)
        {
            if (key.Key == ConsoleKey.Escape)
            {
                return Transition(form with { IsEditing = false, Error = null });
            }

            if (key.Key == ConsoleKey.Enter)
            {
                return CommitDraft(form);
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                return Transition(form with
                {
                    Draft = form.Draft.Length == 0 ? string.Empty : form.Draft[..^1],
                    Error = null
                });
            }

            return char.IsControl(key.KeyChar)
                ? Transition(form)
                : Transition(form with { Draft = form.Draft + key.KeyChar, Error = null });
        }

        if (bindings.MatchesBack(key))
        {
            return new TodoFormTransition(null);
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            var next = Math.Clamp((int)form.Field + offset, 0, Enum.GetValues<TodoFormField>().Length - 1);
            return Transition(form with { Field = (TodoFormField)next, Error = null });
        }

        if (bindings.MatchesOpen(key))
        {
            return Transition(form with
            {
                IsEditing = true,
                Draft = FieldValue(form),
                Error = null
            });
        }

        if (bindings.MatchesSaveForm(key))
        {
            if (string.IsNullOrWhiteSpace(form.Values.Title))
            {
                return Transition(form with { Error = "Title is required." });
            }

            return new TodoFormTransition(
                null,
                form.IsCreate ? TodoEditorOperation.Create : TodoEditorOperation.Update,
                form.ProjectPath,
                form.Target,
                form.Values);
        }

        return Transition(form);
    }

    public TodoContentEditorTransition ReduceContent(
        TodoContentEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (editor.Mode == ContentEditorMode.Edit)
        {
            if (key.Key == ConsoleKey.Escape)
            {
                return ContentTransition(editor with
                {
                    Mode = ContentEditorMode.Browse,
                    IsAdding = false,
                    Draft = string.Empty,
                    Error = null
                });
            }

            if (key.Key == ConsoleKey.Enter)
            {
                return CommitContentDraft(editor);
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                return ContentTransition(editor with
                {
                    Draft = editor.Draft.Length == 0 ? string.Empty : editor.Draft[..^1],
                    Error = null
                });
            }

            return char.IsControl(key.KeyChar)
                ? ContentTransition(editor)
                : ContentTransition(editor with { Draft = editor.Draft + key.KeyChar, Error = null });
        }

        if (editor.Mode == ContentEditorMode.ConfirmRemoval)
        {
            if (bindings.MatchesOpen(key))
            {
                return ContentTransition(RemoveSelectedSubtask(editor) with
                {
                    Mode = ContentEditorMode.Browse,
                    Error = null
                });
            }

            return bindings.MatchesBack(key)
                ? ContentTransition(editor with { Mode = ContentEditorMode.Browse, Error = null })
                : ContentTransition(editor);
        }

        if (bindings.MatchesBack(key))
        {
            return new TodoContentEditorTransition(null);
        }

        if (bindings.MatchesFocusNext(key) || bindings.MatchesFocusPrevious(key))
        {
            var focus = editor.Focus == ContentEditorFocus.Notes
                ? ContentEditorFocus.Subtasks
                : ContentEditorFocus.Notes;
            return ContentTransition(editor with { Focus = focus, Error = null });
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            var updated = editor.Focus == ContentEditorFocus.Notes
                ? editor with
                {
                    NoteIndex = Math.Clamp(editor.NoteIndex + offset, 0, Math.Max(0, editor.Notes.Length - 1)),
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
            return ContentTransition(updated);
        }

        if (bindings.MatchesCreateTodo(key))
        {
            return ContentTransition(editor with
            {
                Mode = ContentEditorMode.Edit,
                IsAdding = true,
                Draft = string.Empty,
                Error = null
            });
        }

        if (bindings.MatchesEditTodo(key) || bindings.MatchesOpen(key))
        {
            var value = editor.Focus == ContentEditorFocus.Notes
                ? editor.Notes.ElementAtOrDefault(editor.NoteIndex)?.Text
                : editor.Subtasks.ElementAtOrDefault(editor.SubtaskIndex)?.Title;
            return value is null
                ? ContentTransition(editor with { Error = "There is no content to edit." })
                : ContentTransition(editor with
                {
                    Mode = ContentEditorMode.Edit,
                    IsAdding = false,
                    Draft = value,
                    Error = null
                });
        }

        if (bindings.MatchesRemoveContent(key))
        {
            if (editor.Focus == ContentEditorFocus.Notes)
            {
                if (editor.Notes.Length == 0)
                {
                    return ContentTransition(editor with { Error = "There is no note to remove." });
                }

                var notes = editor.Notes.RemoveAt(editor.NoteIndex);
                return ContentTransition(editor with
                {
                    Notes = notes,
                    NoteIndex = Math.Clamp(editor.NoteIndex, 0, Math.Max(0, notes.Length - 1)),
                    Error = null
                });
            }

            if (editor.Subtasks.Length == 0)
            {
                return ContentTransition(editor with { Error = "There is no subtask to remove." });
            }

            var selected = editor.Subtasks[editor.SubtaskIndex];
            return selected.DescendantCount > 0
                ? ContentTransition(editor with { Mode = ContentEditorMode.ConfirmRemoval, Error = null })
                : ContentTransition(RemoveSelectedSubtask(editor));
        }

        if (editor.Focus == ContentEditorFocus.Subtasks && bindings.MatchesToggleTodo(key))
        {
            if (editor.Subtasks.Length == 0)
            {
                return ContentTransition(editor with { Error = "There is no subtask to toggle." });
            }

            var selected = editor.Subtasks[editor.SubtaskIndex];
            return ContentTransition(editor with
            {
                Subtasks = editor.Subtasks.SetItem(
                    editor.SubtaskIndex,
                    selected with { IsCompleted = !selected.IsCompleted }),
                Error = null
            });
        }

        if (bindings.MatchesSaveForm(key))
        {
            return new TodoContentEditorTransition(
                null,
                TodoEditorOperation.UpdateContent,
                editor.Target,
                editor.ToUpdate());
        }

        return ContentTransition(editor);
    }

    private static TodoFormTransition CommitDraft(TodoFormState form)
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

        return Transition(form with
        {
            Values = values,
            IsEditing = error is not null,
            Error = error
        });
    }

    private static TodoContentEditorTransition CommitContentDraft(TodoContentEditorState editor)
    {
        var value = editor.Draft.Trim();
        if (value.Length == 0)
        {
            return ContentTransition(editor with { Error = "Content must not be empty." });
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

        return ContentTransition(editor with
        {
            Mode = ContentEditorMode.Browse,
            IsAdding = false,
            Draft = string.Empty,
            Error = null
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

    private static TodoFormTransition Transition(TodoFormState state) => new(state);

    private static TodoContentEditorTransition ContentTransition(TodoContentEditorState state) => new(state);
}
