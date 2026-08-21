using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class TodoBulkEditorReducer(Func<DateOnly>? todayProvider = null)
{
    private readonly Func<DateOnly> todayProvider = todayProvider ??
        (() => DateOnly.FromDateTime(DateTime.Today));

    public TodoBulkEditorTransition Reduce(
        TodoBulkEditorState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (state.FieldTextBox is not null)
        {
            return ReduceTextBox(state, key, bindings);
        }

        if (bindings.MatchesBack(key))
        {
            return new(null, TodoBulkEditorOutcome.Cancelled);
        }

        if (bindings.MatchesSaveForm(key))
        {
            return TryCreateUpdate(state, out var update, out var error)
                ? new(state with { Error = null }, TodoBulkEditorOutcome.Accepted, update)
                : new(state with { Error = error });
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            return new(state with
            {
                SelectedIndex = Math.Clamp(state.SelectedIndex + offset, 0, TodoBulkEditorState.FieldCount - 1),
                Error = null
            });
        }

        if (!bindings.MatchesOpen(key))
        {
            return new(state);
        }

        if (state.SelectedField == TodoBulkEditorField.Completion)
        {
            return new(state with { Complete = !state.Complete, Error = null });
        }

        return new(state with
        {
            FieldTextBox = TextBox.Create(
                FieldLabel(state.SelectedField),
                editable: true,
                FieldValue(state),
                isActive: true),
            Error = null
        });
    }

    public bool TryCreateUpdate(
        TodoBulkEditorState state,
        out TodoBulkUpdate? update,
        out string? error)
    {
        error = null;
        var scheduleMode = TodoBulkScheduleMode.Unchanged;
        DateOnly? date = null;
        var scheduleText = state.ScheduledDate.Trim();
        if (scheduleText == "-")
        {
            scheduleMode = TodoBulkScheduleMode.Clear;
        }
        else if (scheduleText.Length > 0)
        {
            if (!DateExpression.TryParse(scheduleText, todayProvider(), out var parsedDate))
            {
                update = null;
                error = "Scheduled date must be YYYY-MM-DD, t, t+N, w+N, a weekday, empty, or -.";
                return false;
            }

            scheduleMode = TodoBulkScheduleMode.SetDate;
            date = parsedDate;
        }

        if (!TryParseTags(state.Tags, out var tagMode, out var tags, out error))
        {
            update = null;
            return false;
        }

        var priorityMode = TodoBulkPriorityMode.Unchanged;
        TodoPriority? priority = null;
        var priorityText = state.Priority.Trim();
        if (priorityText == "-")
        {
            priorityMode = TodoBulkPriorityMode.Clear;
        }
        else if (priorityText.Length > 0)
        {
            if (!Enum.TryParse<TodoPriority>(priorityText, true, out var parsedPriority))
            {
                update = null;
                error = "Priority must be Highest, High, Medium, Low, Lowest, empty, or -.";
                return false;
            }

            priorityMode = TodoBulkPriorityMode.Set;
            priority = parsedPriority;
        }

        update = new TodoBulkUpdate(
            scheduleMode,
            date,
            tagMode,
            tags,
            priorityMode,
            priority,
            state.Complete);
        if (!update.HasChanges)
        {
            update = null;
            error = "Choose at least one bulk change.";
            return false;
        }

        return true;
    }

    private TodoBulkEditorTransition ReduceTextBox(
        TodoBulkEditorState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        var transition = TextBox.Default.Reduce(state.FieldTextBox!, key, bindings);
        if (transition.Outcome == TextBoxOutcome.Cancelled)
        {
            return new(state with { FieldTextBox = null, Error = null });
        }

        if (transition.Outcome != TextBoxOutcome.Accepted)
        {
            return new(state with { FieldTextBox = transition.State, Error = null });
        }

        var value = transition.State!.Text.Trim();
        var updated = state.SelectedField switch
        {
            TodoBulkEditorField.ScheduledDate => state with { ScheduledDate = value },
            TodoBulkEditorField.Tags => state with { Tags = value },
            TodoBulkEditorField.Priority => state with { Priority = value },
            _ => state
        };
        return new(updated with { FieldTextBox = null, Error = null });
    }

    private static bool TryParseTags(
        string value,
        out TodoBulkTagMode mode,
        out ImmutableArray<string> tags,
        out string? error)
    {
        var text = value.Trim();
        if (text.Length == 0)
        {
            mode = TodoBulkTagMode.Unchanged;
            tags = [];
            error = null;
            return true;
        }

        mode = text[0] switch
        {
            '+' => TodoBulkTagMode.Add,
            '-' => TodoBulkTagMode.Remove,
            '=' => TodoBulkTagMode.Replace,
            _ => TodoBulkTagMode.Unchanged
        };
        if (mode == TodoBulkTagMode.Unchanged)
        {
            tags = [];
            error = "Tags must start with + to add, - to remove, or = to replace.";
            return false;
        }

        tags = [.. text[1..]
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim().TrimStart('#'))
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        if (mode is TodoBulkTagMode.Add or TodoBulkTagMode.Remove && tags.Length == 0)
        {
            error = "Add and remove tag updates require at least one tag.";
            return false;
        }

        error = null;
        return true;
    }

    private static string FieldValue(TodoBulkEditorState state) => state.SelectedField switch
    {
        TodoBulkEditorField.ScheduledDate => state.ScheduledDate,
        TodoBulkEditorField.Tags => state.Tags,
        TodoBulkEditorField.Priority => state.Priority,
        _ => string.Empty
    };

    private static string FieldLabel(TodoBulkEditorField field) => field switch
    {
        TodoBulkEditorField.ScheduledDate => "Scheduled date (empty unchanged, - clear)",
        TodoBulkEditorField.Tags => "Tags (+ add, - remove, = replace)",
        TodoBulkEditorField.Priority => "Priority (empty unchanged, - clear)",
        _ => "Complete"
    };
}
