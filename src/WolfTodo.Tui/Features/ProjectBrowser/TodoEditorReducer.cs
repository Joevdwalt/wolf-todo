using System.Collections.Immutable;
using System.Globalization;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoEditorProjectOption(string Title, string Path);

public enum TodoEditorOperation
{
    None,
    Create,
    Update
}

public sealed record TodoEditorTransition(
    TodoTaskEditorState? State,
    TodoEditorOperation Operation = TodoEditorOperation.None,
    string? ProjectPath = null,
    TodoIdentity? Target = null,
    TodoTaskUpdate? Update = null);

public sealed class TodoEditorReducer
{
    private readonly Func<DateOnly> todayProvider;

    public TodoEditorReducer(Func<DateOnly>? todayProvider = null)
    {
        this.todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
    }

    public TodoTaskEditorState CreateEditor(
        string? projectPath,
        bool hasProjects,
        TodoSchedule? schedule = null,
        TodoScheduleRequirement scheduleRequirement = TodoScheduleRequirement.None,
        TimeSpan? duration = null) =>
        TodoTaskEditorState.Create(projectPath, hasProjects, schedule, scheduleRequirement, duration);

    public TodoTaskEditorState EditEditor(TodoItem todo, TodoIdentity identity) =>
        TodoTaskEditorState.Edit(todo, identity);

    public TodoEditorTransition Reduce(
        TodoTaskEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        IReadOnlyList<TodoEditorProjectOption> projects)
    {
        if (editor.IsChoosingProject)
        {
            return ReduceProjectPicker(editor, key, bindings, projects);
        }

        if (editor.ContentTextBox is not null)
        {
            return ReduceContentTextBox(editor, key, bindings);
        }

        if (editor.Mode == TodoTaskEditorMode.Edit)
        {
            return ReduceDraft(editor, key);
        }

        if (editor.Mode == TodoTaskEditorMode.ChooseContentType)
        {
            return ReduceContentTypePicker(editor, key, bindings);
        }

        if (editor.Mode == TodoTaskEditorMode.ConfirmRemoval)
        {
            if (bindings.MatchesOpen(key))
            {
                return Transition(RemoveSelectedContent(editor) with
                {
                    Mode = TodoTaskEditorMode.Browse,
                    Error = null
                });
            }

            return bindings.MatchesBack(key)
                ? Transition(editor with { Mode = TodoTaskEditorMode.Browse, Error = null })
                : Transition(editor);
        }

        if (bindings.MatchesBack(key))
        {
            return new TodoEditorTransition(null);
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            return Transition(editor with
            {
                SelectedIndex = Math.Clamp(
                    editor.SelectedIndex + offset,
                    0,
                    Math.Max(0, editor.SelectableCount - 1)),
                Error = null
            });
        }

        if (bindings.MatchesCreateTodo(key))
        {
            return Transition(editor with
            {
                Mode = TodoTaskEditorMode.ChooseContentType,
                AddKind = ContentItemKind.Note,
                Error = null
            });
        }

        if (bindings.MatchesEditTodo(key) || bindings.MatchesEditTodoContent(key) || bindings.MatchesOpen(key))
        {
            if (!editor.IsFieldSelected)
            {
                var selected = editor.Items[editor.SelectedContentIndex];
                return Transition(editor with
                {
                    ContentTextBox = TextBoxState.Create(
                        SelectedValue(editor),
                        selected is ContentNoteDraft),
                    Error = null
                });
            }

            return Transition(editor with
            {
                Mode = TodoTaskEditorMode.Edit,
                IsAddingContent = false,
                Draft = SelectedValue(editor),
                Error = null
            });
        }

        if (bindings.MatchesRemoveContent(key))
        {
            if (editor.IsFieldSelected)
            {
                return Transition(editor with { Error = "Select a note or subtask to remove." });
            }

            var selected = editor.Items[editor.SelectedContentIndex];
            return selected is ContentSubtaskDraft { DescendantCount: > 0 }
                ? Transition(editor with { Mode = TodoTaskEditorMode.ConfirmRemoval, Error = null })
                : Transition(RemoveSelectedContent(editor));
        }

        if (bindings.MatchesToggleTodo(key))
        {
            if (editor.IsFieldSelected)
            {
                return Transition(editor with { Error = "Select a subtask to change completion." });
            }

            var selected = editor.Items[editor.SelectedContentIndex];
            if (selected is not ContentSubtaskDraft subtask)
            {
                return Transition(editor with { Error = "Only subtasks can be completed." });
            }

            return Transition(editor with
            {
                Items = editor.Items.SetItem(
                    editor.SelectedContentIndex,
                    subtask with { IsCompleted = !subtask.IsCompleted }),
                Error = null
            });
        }

        if (bindings.MatchesSaveForm(key))
        {
            if (string.IsNullOrWhiteSpace(editor.Values.Title))
            {
                return Transition(editor with { Error = "Title is required." });
            }

            var schedule = ParseSchedule(editor, todayProvider(), out var scheduleError);
            if (scheduleError is not null)
            {
                return Transition(editor with { Error = scheduleError });
            }

            return new TodoEditorTransition(
                null,
                editor.IsCreate ? TodoEditorOperation.Create : TodoEditorOperation.Update,
                editor.ProjectPath,
                editor.Target,
                editor.ToUpdate(schedule));
        }

        return Transition(editor);
    }

    private static TodoEditorTransition ReduceProjectPicker(
        TodoTaskEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        IReadOnlyList<TodoEditorProjectOption> projects)
    {
        if (bindings.MatchesBack(key))
        {
            return new TodoEditorTransition(null);
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            return Transition(editor with
            {
                ProjectPickerIndex = Math.Clamp(
                    editor.ProjectPickerIndex + offset,
                    0,
                    Math.Max(0, projects.Count - 1)),
                Error = null
            });
        }

        if (bindings.MatchesOpen(key) && projects.Count > 0)
        {
            return Transition(editor with
            {
                ProjectPath = projects[editor.ProjectPickerIndex].Path,
                Error = null
            });
        }

        return Transition(editor);
    }

    private TodoEditorTransition ReduceDraft(TodoTaskEditorState editor, ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return Transition(editor with
            {
                Mode = TodoTaskEditorMode.Browse,
                AddKind = ContentItemKind.Note,
                IsAddingContent = false,
                Draft = string.Empty,
                Error = null
            });
        }

        if (key.Key == ConsoleKey.Enter)
        {
            return CommitDraft(editor);
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            return Transition(editor with
            {
                Draft = editor.Draft.Length == 0 ? string.Empty : editor.Draft[..^1],
                Error = null
            });
        }

        return char.IsControl(key.KeyChar)
            ? Transition(editor)
            : Transition(editor with { Draft = editor.Draft + key.KeyChar, Error = null });
    }

    private static TodoEditorTransition ReduceContentTypePicker(
        TodoTaskEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (bindings.MatchesBack(key))
        {
            return Transition(editor with
            {
                Mode = TodoTaskEditorMode.Browse,
                AddKind = ContentItemKind.Note,
                Error = null
            });
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            return Transition(editor with
            {
                AddKind = (ContentItemKind)Math.Clamp(
                    (int)editor.AddKind + offset,
                    0,
                    Enum.GetValues<ContentItemKind>().Length - 1),
                Error = null
            });
        }

        if (bindings.MatchesOpen(key))
        {
            return Transition(editor with
            {
                Mode = TodoTaskEditorMode.Browse,
                IsAddingContent = true,
                ContentTextBox = TextBoxState.Create(string.Empty, editor.AddKind == ContentItemKind.Note),
                Error = null
            });
        }

        return Transition(editor);
    }

    private TodoEditorTransition CommitDraft(TodoTaskEditorState editor)
    {
        var value = editor.Draft.Trim();
        if (editor.IsAddingContent)
        {
            if (value.Length == 0)
            {
                return Transition(editor with { Error = "Content must not be empty." });
            }

            var item = editor.AddKind == ContentItemKind.Note
                ? (ContentItemDraft)new ContentNoteDraft(null, value)
                : new ContentSubtaskDraft(null, value, false, 0);
            var insertionIndex = editor.IsFieldSelected || editor.Items.Length == 0
                ? editor.Items.Length
                : Math.Min(editor.SelectedContentIndex + 1, editor.Items.Length);
            editor = editor with
            {
                Items = editor.Items.Insert(insertionIndex, item),
                SelectedIndex = TodoTaskEditorState.FieldCount + insertionIndex
            };
        }
        else if (editor.IsFieldSelected)
        {
            editor = CommitField(editor, value);
            if (editor.Error is not null)
            {
                return Transition(editor);
            }
        }
        else
        {
            if (value.Length == 0)
            {
                return Transition(editor with { Error = "Content must not be empty." });
            }

            var selected = editor.Items[editor.SelectedContentIndex];
            var updated = selected switch
            {
                ContentNoteDraft note => (ContentItemDraft)(note with { Text = value }),
                ContentSubtaskDraft subtask => subtask with { Title = value },
                _ => throw new InvalidOperationException("Unsupported todo content item.")
            };
            editor = editor with
            {
                Items = editor.Items.SetItem(editor.SelectedContentIndex, updated)
            };
        }

        return Transition(editor with
        {
            Mode = TodoTaskEditorMode.Browse,
            AddKind = ContentItemKind.Note,
            IsAddingContent = false,
            Draft = string.Empty,
            Error = null
        });
    }

    private TodoEditorTransition ReduceContentTextBox(
        TodoTaskEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return Transition(editor with { ContentTextBox = null, IsAddingContent = false, Error = null });
        }

        if (bindings.MatchesSaveForm(key))
        {
            var value = editor.ContentTextBox!.Text.Trim();
            if (value.Length == 0)
            {
                return Transition(editor with { Error = "Content must not be empty." });
            }

            if (editor.IsAddingContent)
            {
                var item = editor.AddKind == ContentItemKind.Note
                    ? (ContentItemDraft)new ContentNoteDraft(null, value)
                    : new ContentSubtaskDraft(null, value, false, 0);
                var insertionIndex = editor.IsFieldSelected || editor.Items.Length == 0
                    ? editor.Items.Length
                    : Math.Min(editor.SelectedContentIndex + 1, editor.Items.Length);
                editor = editor with
                {
                    Items = editor.Items.Insert(insertionIndex, item),
                    SelectedIndex = TodoTaskEditorState.FieldCount + insertionIndex
                };
            }
            else
            {
                var selected = editor.Items[editor.SelectedContentIndex];
                var item = selected switch
                {
                    ContentNoteDraft note => (ContentItemDraft)(note with { Text = value }),
                    ContentSubtaskDraft subtask => subtask with { Title = value },
                    _ => throw new InvalidOperationException("Unsupported todo content item.")
                };
                editor = editor with { Items = editor.Items.SetItem(editor.SelectedContentIndex, item) };
            }

            return Transition(editor with
            {
                Mode = TodoTaskEditorMode.Browse,
                ContentTextBox = null,
                IsAddingContent = false,
                AddKind = ContentItemKind.Note,
                Error = null
            });
        }

        return Transition(editor with { ContentTextBox = TextBoxReducer.Reduce(editor.ContentTextBox!, key), Error = null });
    }

    private TodoTaskEditorState CommitField(TodoTaskEditorState editor, string value)
    {
        var values = editor.Values;
        string? error = null;
        switch (editor.SelectedField)
        {
            case TodoFormField.Title:
                values = values with { Title = value };
                if (value.Length == 0)
                {
                    error = "Title is required.";
                }
                break;
            case TodoFormField.Reference:
                values = values with { ExternalReference = NullIfEmpty(value) };
                break;
            case TodoFormField.Priority:
                if (value.Length == 0)
                {
                    values = values with { Priority = null };
                }
                else if (Enum.TryParse<TodoPriority>(value, true, out var priority))
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
                    Tags = value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                        .Select(tag => tag.Trim().TrimStart('#'))
                        .Where(tag => tag.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToImmutableArray()
                };
                break;
            case TodoFormField.ScheduledDate:
                editor = editor with { ScheduledDate = ParseDateText(value, todayProvider(), out error) };
                break;
            case TodoFormField.ScheduledTime:
                editor = editor with { ScheduledTime = ParseTimeText(value, out error) };
                break;
            case TodoFormField.Duration:
                values = values with { Duration = ParseDurationText(value, out error) };
                break;
        }

        return editor with { Values = values, Error = error };
    }

    private static TodoTaskEditorState RemoveSelectedContent(TodoTaskEditorState editor)
    {
        var items = editor.Items.RemoveAt(editor.SelectedContentIndex);
        return editor with
        {
            Items = items,
            SelectedIndex = Math.Clamp(
                editor.SelectedIndex,
                0,
                TodoTaskEditorState.FieldCount + items.Length - 1),
            Error = null
        };
    }

    private static string SelectedValue(TodoTaskEditorState editor)
    {
        if (!editor.IsFieldSelected)
        {
            return editor.Items[editor.SelectedContentIndex] switch
            {
                ContentNoteDraft note => note.Text,
                ContentSubtaskDraft subtask => subtask.Title,
                _ => string.Empty
            };
        }

        return editor.SelectedField switch
        {
            TodoFormField.Title => editor.Values.Title,
            TodoFormField.Reference => editor.Values.ExternalReference ?? string.Empty,
            TodoFormField.Priority => editor.Values.Priority?.ToString() ?? string.Empty,
            TodoFormField.Tags => string.Join(' ', editor.Values.Tags.Select(tag => $"#{tag}")),
            TodoFormField.ScheduledDate => editor.ScheduledDate,
            TodoFormField.ScheduledTime => editor.ScheduledTime,
            TodoFormField.Duration => editor.Duration,
            _ => string.Empty
        };
    }

    private static TodoSchedule? ParseSchedule(TodoTaskEditorState editor, DateOnly today, out string? error)
    {
        error = null;
        var hasDate = !string.IsNullOrWhiteSpace(editor.ScheduledDate);
        var hasTime = !string.IsNullOrWhiteSpace(editor.ScheduledTime);
        if (!hasDate && !hasTime)
        {
            if (editor.ScheduleRequirement != TodoScheduleRequirement.None)
            {
                error = editor.ScheduleRequirement == TodoScheduleRequirement.Date
                    ? "A scheduled date is required."
                    : "A scheduled date and time are required.";
            }

            return null;
        }

        if (!hasDate)
        {
            error = "A scheduled time requires a scheduled date.";
            return null;
        }

        if (!DateExpression.TryParse(editor.ScheduledDate, today, out var date))
        {
            error = "Schedule date must use YYYY-MM-DD, t, t+N, or w+N.";
            return null;
        }

        if (!hasTime)
        {
            if (editor.ScheduleRequirement == TodoScheduleRequirement.DateAndTime)
            {
                error = "A scheduled date and time are required.";
                return null;
            }

            return new TodoSchedule(date);
        }

        if (!TimeOnly.TryParseExact(
                editor.ScheduledTime,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time))
        {
            error = "Schedule must use YYYY-MM-DD and HH:mm.";
            return null;
        }

        if (time.Minute is not (0 or 15 or 30 or 45) || time < new TimeOnly(6, 0) || time > new TimeOnly(21, 45))
        {
            error = "Scheduled time must be a quarter-hour from 06:00 through 21:45.";
            return null;
        }

        return new TodoSchedule(date, time);
    }

    private static string ParseDateText(string value, DateOnly today, out string? error)
    {
        error = null;
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (DateExpression.TryParse(value, today, out var date))
        {
            return date.ToString("yyyy-MM-dd");
        }

        error = "Date must use YYYY-MM-DD, t, t+N, w+N, or be empty.";
        return value;
    }

    private static string ParseTimeText(string value, out string? error)
    {
        error = null;
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (TimeOnly.TryParseExact(
                value,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time) &&
            time.Minute is 0 or 15 or 30 or 45 &&
            time >= new TimeOnly(6, 0) &&
            time <= new TimeOnly(21, 45))
        {
            return time.ToString("HH:mm");
        }

        error = "Time must use HH:mm on a quarter-hour from 06:00 through 21:45, or be empty.";
        return value;
    }

    private static TimeSpan? ParseDurationText(string value, out string? error)
    {
        error = null;
        if (value.Length == 0)
        {
            return null;
        }

        var number = value.EndsWith('m') ? value[..^1] : value;
        if (int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) &&
            minutes is >= 15 and <= 960 && minutes % 15 == 0)
        {
            return TimeSpan.FromMinutes(minutes);
        }

        error = "Duration must be a 15-minute value from 15m through 960m, or be empty.";
        return null;
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private static TodoEditorTransition Transition(TodoTaskEditorState state) => new(state);
}
