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

        if (editor.FieldTextBox is not null)
        {
            return ReduceFieldTextBox(editor, key, bindings);
        }

        if (editor.SubtaskTextBox is not null)
        {
            return ReduceSubtaskTextBox(editor, key, bindings);
        }

        if (editor.Mode == TodoTaskEditorMode.ConfirmRemoval)
        {
            if (bindings.MatchesOpen(key))
            {
                return Transition(RemoveSelectedSubtask(editor) with
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

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key) ||
            bindings.MatchesFocusPrevious(key) || bindings.MatchesFocusNext(key))
        {
            var offset = bindings.MatchesMoveUp(key) || bindings.MatchesFocusPrevious(key) ? -1 : 1;
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
            return BeginSubtaskAdd(editor);
        }

        if (bindings.MatchesEditTodo(key) ||
            bindings.MatchesEditTodoContent(key) ||
            bindings.MatchesOpen(key))
        {
            if (editor.IsFieldSelected)
            {
                return Transition(editor with
                {
                    FieldTextBox = TextBox.Create(
                        FieldLabel(editor.SelectedField),
                        true,
                        SelectedFieldValue(editor),
                        isActive: true),
                    Error = null
                });
            }

            if (editor.IsContentSelected)
            {
                return Transition(editor with
                {
                    ContentTextBox = MultilineTextBoxState.Create("Content", editor.Content, true),
                    Error = null
                });
            }

            return BeginSubtaskEdit(editor);
        }

        if (bindings.MatchesRemoveContent(key))
        {
            if (!editor.IsSubtaskSelected)
            {
                return Transition(editor with { Error = "Select a subtask to remove." });
            }

            var selected = editor.Subtasks[editor.SelectedSubtaskIndex];
            return selected.DescendantCount > 0
                ? Transition(editor with { Mode = TodoTaskEditorMode.ConfirmRemoval, Error = null })
                : Transition(RemoveSelectedSubtask(editor));
        }

        if (bindings.MatchesToggleTodo(key))
        {
            if (!editor.IsSubtaskSelected)
            {
                return Transition(editor with { Error = "Select a subtask to change completion." });
            }

            var index = editor.SelectedSubtaskIndex;
            var subtask = editor.Subtasks[index];
            return Transition(editor with
            {
                Subtasks = editor.Subtasks.SetItem(index, subtask with { IsCompleted = !subtask.IsCompleted }),
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
        var transition = SelectList.Default.Reduce(
            new SelectListView(
                "Choose project",
                projects.Select(project => new SelectOption(project.Title)).ToArray(),
                editor.ProjectPickerIndex,
                null,
                "No projects available.",
                string.Empty),
            key,
            bindings);

        return transition.Outcome switch
        {
            SelectListOutcome.Cancelled => new TodoEditorTransition(null),
            SelectListOutcome.SelectionChanged => Transition(editor with
            {
                ProjectPickerIndex = transition.State!.SelectedIndex,
                Error = null
            }),
            SelectListOutcome.Accepted => Transition(editor with
            {
                ProjectPath = projects[transition.State!.ClampedSelectedIndex].Path,
                Error = null
            }),
            _ => Transition(editor)
        };
    }

    private TodoEditorTransition ReduceFieldTextBox(
        TodoTaskEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        var transition = TextBox.Default.Reduce(editor.FieldTextBox!, key, bindings);
        if (transition.Outcome == TextBoxOutcome.Cancelled)
        {
            return Transition(editor with { FieldTextBox = null, Error = null });
        }

        if (transition.Outcome == TextBoxOutcome.Accepted)
        {
            var updated = CommitField(editor, transition.State!.Text.Trim());
            if (updated.Error is not null)
            {
                return Transition(updated with { Mode = TodoTaskEditorMode.Edit, FieldTextBox = transition.State });
            }

            return Transition(updated with { FieldTextBox = null, Error = null });
        }

        return Transition(editor with { FieldTextBox = transition.State, Error = null });
    }

    private TodoEditorTransition ReduceContentTextBox(
        TodoTaskEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        var transition = MultilineTextBox.Default.Reduce(editor.ContentTextBox!, key, bindings);
        if (transition.Outcome == MultilineTextBoxOutcome.Cancelled)
        {
            return Transition(editor with { ContentTextBox = null, Error = null });
        }

        if (transition.Outcome == MultilineTextBoxOutcome.Accepted)
        {
            return Transition(editor with
            {
                Content = transition.State!.Text.Trim(),
                ContentTextBox = null,
                Error = null
            });
        }

        return Transition(editor with { ContentTextBox = transition.State, Error = null });
    }

    private TodoEditorTransition ReduceSubtaskTextBox(
        TodoTaskEditorState editor,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        var transition = TextBox.Default.Reduce(editor.SubtaskTextBox!, key, bindings);
        if (transition.Outcome == TextBoxOutcome.Cancelled)
        {
            return Transition(editor with
            {
                SubtaskTextBox = null,
                IsAddingSubtask = false,
                Error = null
            });
        }

        if (transition.Outcome != TextBoxOutcome.Accepted)
        {
            return Transition(editor with { SubtaskTextBox = transition.State, Error = null });
        }

        var title = transition.State!.Text.Trim();
        if (title.Length == 0)
        {
            return Transition(editor with { SubtaskTextBox = transition.State, Error = "Subtask title must not be empty." });
        }

        if (title.IndexOfAny(['\r', '\n']) >= 0)
        {
            return Transition(editor with { SubtaskTextBox = transition.State, Error = "Subtask title must stay on one line." });
        }

        if (editor.IsAddingSubtask)
        {
            var insertionIndex = editor.IsSubtaskSelected
                ? editor.SelectedSubtaskIndex + 1
                : editor.Subtasks.Length;
            var updated = editor with
            {
                Subtasks = editor.Subtasks.Insert(insertionIndex, new TodoSubtaskDraft(null, title, false, 0)),
                SelectedIndex = TodoTaskEditorState.ContentIndex + 1 + insertionIndex
            };
            return Transition(updated with
            {
                SubtaskTextBox = null,
                IsAddingSubtask = false,
                Error = null
            });
        }

        var index = editor.SelectedSubtaskIndex;
        return Transition(editor with
        {
            Subtasks = editor.Subtasks.SetItem(index, editor.Subtasks[index] with { Title = title }),
            SubtaskTextBox = null,
            Error = null
        });
    }

    private static TodoEditorTransition BeginSubtaskAdd(TodoTaskEditorState editor) =>
        Transition(editor with
        {
            Mode = TodoTaskEditorMode.Edit,
            IsAddingSubtask = true,
            SubtaskTextBox = TextBox.Create("Add subtask", true, string.Empty, isActive: true),
            Error = null
        });

    private static TodoEditorTransition BeginSubtaskEdit(TodoTaskEditorState editor) =>
        Transition(editor with
        {
            SubtaskTextBox = TextBox.Create(
                "Subtask",
                true,
                editor.Subtasks[editor.SelectedSubtaskIndex].Title,
                isActive: true),
            Error = null
        });

    private TodoTaskEditorState CommitField(TodoTaskEditorState editor, string value)
    {
        var values = editor.Values;
        string? error = null;
        switch (editor.SelectedField)
        {
            case TodoFormField.Title:
                values = values with { Title = value };
                if (value.Length == 0) error = "Title is required.";
                break;
            case TodoFormField.Reference:
                values = values with { ExternalReference = NullIfEmpty(value) };
                break;
            case TodoFormField.Priority:
                if (value.Length == 0)
                    values = values with { Priority = null };
                else if (Enum.TryParse<TodoPriority>(value, true, out var priority))
                    values = values with { Priority = priority };
                else
                    error = "Priority must be Highest, High, Medium, Low, Lowest, or empty.";
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

    private static TodoTaskEditorState RemoveSelectedSubtask(TodoTaskEditorState editor)
    {
        var subtasks = editor.Subtasks.RemoveAt(editor.SelectedSubtaskIndex);
        return editor with
        {
            Subtasks = subtasks,
            SelectedIndex = Math.Clamp(editor.SelectedIndex, 0, TodoTaskEditorState.ContentIndex + subtasks.Length),
            Error = null
        };
    }

    private static string SelectedFieldValue(TodoTaskEditorState editor) => editor.SelectedField switch
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

    private static string FieldLabel(TodoFormField field) => field switch
    {
        TodoFormField.Title => "Title",
        TodoFormField.Reference => "Reference",
        TodoFormField.Priority => "Priority",
        TodoFormField.Tags => "Tags",
        TodoFormField.ScheduledDate => "Scheduled date (YYYY-MM-DD, t+1, w+1, mon)",
        TodoFormField.ScheduledTime => "Scheduled time",
        TodoFormField.Duration => "Duration",
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private static TodoSchedule? ParseSchedule(TodoTaskEditorState editor, DateOnly today, out string? error)
    {
        error = null;
        var hasDate = !string.IsNullOrWhiteSpace(editor.ScheduledDate);
        var hasTime = !string.IsNullOrWhiteSpace(editor.ScheduledTime);
        if (!hasDate && !hasTime)
        {
            if (editor.ScheduleRequirement != TodoScheduleRequirement.None)
                error = editor.ScheduleRequirement == TodoScheduleRequirement.Date
                    ? "A scheduled date is required."
                    : "A scheduled date and time are required.";
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

        if (!TimeOnly.TryParseExact(editor.ScheduledTime, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
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
        if (value.Length == 0) return string.Empty;
        if (DateExpression.TryParse(value, today, out var date)) return date.ToString("yyyy-MM-dd");
        error = "Date must use YYYY-MM-DD, t, t+N, w+N, or be empty.";
        return value;
    }

    private static string ParseTimeText(string value, out string? error)
    {
        error = null;
        if (value.Length == 0) return string.Empty;
        if (TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) &&
            time.Minute is 0 or 15 or 30 or 45 && time >= new TimeOnly(6, 0) && time <= new TimeOnly(21, 45))
            return time.ToString("HH:mm");
        error = "Time must use HH:mm on a quarter-hour from 06:00 through 21:45, or be empty.";
        return value;
    }

    private static TimeSpan? ParseDurationText(string value, out string? error)
    {
        error = null;
        if (value.Length == 0) return null;
        var number = value.EndsWith('m') ? value[..^1] : value;
        if (int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) &&
            minutes is >= 15 and <= 960 && minutes % 15 == 0)
            return TimeSpan.FromMinutes(minutes);
        error = "Duration must be a 15-minute value from 15m through 960m, or be empty.";
        return null;
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private static TodoEditorTransition Transition(TodoTaskEditorState state) => new(state);
}
