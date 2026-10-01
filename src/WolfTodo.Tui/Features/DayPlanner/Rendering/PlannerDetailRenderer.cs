using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerDetailRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly CalendarItemRenderer calendarItemRenderer = new();
    private readonly TodoRowRenderer todoRowRenderer = new();

    public IReadOnlyList<IRenderable> PlannerDetailLines(PlannerView view, TuiTheme theme)
    {
        if (view.State.Mode == PlannerMode.MoveTodo && view.State.MovingTodo is { } movingIdentity)
        {
            var moving = view.Slots
                .SelectMany(slot => slot.Assignments)
                .Concat(view.CalendarAgenda.AllDayItems
                    .Where(item => item.Assignment is not null)
                    .Select(item => item.Assignment!))
                .FirstOrDefault(assignment => assignment.Identity == movingIdentity);
            if (moving is not null)
            {
                var duration = moving.Todo.Duration;
                var destination = FormatMoveDestination(view, duration);
                var current = FormatCurrentSchedule(moving.Todo.Schedule, duration);
                return
                [
                    new Text("MOVE TASK", themeRenderer.Style(theme.AccentBright, Decoration.Bold)),
                    new Text($"Task: {moving.Todo.Title}", themeRenderer.Style(theme.Text)),
                    new Text($"LINK: {TaskLinkCode.Generate(moving.Identity.ProjectPath, moving.Identity.SourceLine)}", themeRenderer.Style(theme.Info)),
                    new Text($"Current: {current}", themeRenderer.Style(theme.Date)),
                    new Text($"Destination: {destination}", themeRenderer.Style(theme.Date)),
                    new Text($"Duration: {todoRowRenderer.FormatDuration(duration) ?? "Instant"}", themeRenderer.Style(theme.Info))
                ];
            }
        }

        if (view.State.Focus == PlannerFocus.AllDay)
        {
            return calendarItemRenderer.AllDayDetailLines(view, theme);
        }

        if (view.SelectedAssignment is null)
        {
            return view.SelectedMeeting is null
                ? [new Text("EMPTY TIMESLOT", themeRenderer.Style(theme.Muted, Decoration.Dim))]
                : calendarItemRenderer.MeetingDetailLines(view, theme);
        }

        var assignment = view.SelectedAssignment;
        var todo = assignment.Todo;
        var lines = new List<IRenderable>
        {
            new Text(todo.Title, themeRenderer.Style(theme.Heading, Decoration.Bold))
        };
        if (view.SelectedSlot.Assignments.Length > 1)
        {
            lines.Add(new Text(
                $"{view.SelectedSlot.Assignments.Length} STACKED TASKS · J/K SELECT",
                themeRenderer.Style(theme.Info, Decoration.Bold)));
        }

        calendarItemRenderer.AddField(lines, "Project", assignment.ProjectTitle, theme, theme.Text);
        calendarItemRenderer.AddField(lines, "Link", TaskLinkCode.Generate(assignment.Identity.ProjectPath, assignment.Identity.SourceLine), theme, theme.Info);
        if (!string.IsNullOrEmpty(todo.SectionPath))
        {
            calendarItemRenderer.AddField(lines, "Section", todo.SectionPath, theme, theme.Text);
        }

        calendarItemRenderer.AddField(lines, "Reference", todo.ExternalReference, theme, theme.Info);
        calendarItemRenderer.AddField(lines, "Priority", todo.Priority?.ToString(), theme, todoRowRenderer.PriorityColor(todo.Priority, theme));
        calendarItemRenderer.AddField(
            lines,
            "Tags",
            todo.Tags.Length == 0 ? null : string.Join(", ", todo.Tags.Select(tag => $"#{tag}")),
            theme,
            theme.Tag);
        calendarItemRenderer.AddField(
            lines,
            "Scheduled",
            todo.Schedule is null ? null : todoRowRenderer.FormatSchedule(todo.Schedule),
            theme,
            theme.Date);
        calendarItemRenderer.AddField(lines, "Duration", todoRowRenderer.FormatDuration(todo.Duration), theme, theme.Info);
        calendarItemRenderer.AddField(
            lines,
            "Calendar",
            view.SelectedSlot.Meetings.FirstOrDefault() is null
                ? null
                : calendarItemRenderer.MeetingLabel(view.SelectedSlot.Meetings[0]) +
                  (view.SelectedSlot.Meetings.Length > 1 ? $" +{view.SelectedSlot.Meetings.Length - 1}" : string.Empty),
            theme,
            theme.Info);

        if (todo.Notes.Length > 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("NOTES", themeRenderer.Style(theme.Heading, Decoration.Bold)));
            lines.AddRange(todo.Notes.Select(note => new Text($"• {note.Text}", themeRenderer.Style(theme.Text))));
        }

        if (todo.Subtasks.Length > 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("SUBTASKS", themeRenderer.Style(theme.Heading, Decoration.Bold)));
            lines.AddRange(todo.Subtasks.Select(subtask => todoRowRenderer.DetailLine(subtask, [], false, theme)));
        }

        if (todo.Notes.Length == 0 && todo.Subtasks.Length == 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("NO ADDITIONAL DETAILS", themeRenderer.Style(theme.Muted, Decoration.Dim)));
        }

        return lines;
    }

    public IRenderable PlannerCompactDetail(PlannerView view, TuiTheme theme)
    {
        if (view.State.ViewMode == PlannerViewMode.MultiDay)
        {
            return PlannerMultiDayCompactDetail(view, theme);
        }

        if (view.State.Focus == PlannerFocus.AllDay)
        {
            var item = view.SelectedAllDayItem;
            if (item is null)
            {
                return new Text("Empty all-day schedule", themeRenderer.Style(theme.Muted, Decoration.Dim));
            }

            var label = item.Assignment is null
                ? $"{item.Title}  ·  {calendarItemRenderer.AllDayKindLabel(item.Kind)}  ·  READ ONLY"
                : $"LINK: {TaskLinkCode.Generate(item.Assignment.Identity.ProjectPath, item.Assignment.Identity.SourceLine)}  ·  {item.Title}  ·  {item.ProjectTitle}  ·  ALL DAY";
            return new Text(
                label,
                themeRenderer.Style(item.Assignment is null ? theme.Info : theme.Heading, Decoration.Bold)).Ellipsis();
        }

        if (view.SelectedAssignment is null)
        {
            if (view.SelectedMeeting is null)
            {
                return new Text("Empty timeslot", themeRenderer.Style(theme.Muted, Decoration.Dim));
            }

            var meeting = view.SelectedMeeting;
            var meetingLine = new System.Text.StringBuilder();
            themeRenderer.AppendStyled(meetingLine, meeting.Title, theme.Info, Decoration.Bold);
            themeRenderer.AppendStyled(meetingLine, $"  {calendarItemRenderer.MeetingTimeAndDuration(meeting)}", theme.Muted, Decoration.Dim);
            return new Markup(meetingLine.ToString()).Ellipsis();
        }

        var assignment = view.SelectedAssignment;
        var todo = assignment.Todo;
        var metadata = new[]
        {
            assignment.ProjectTitle,
            todo.Priority?.ToString(),
            todo.Tags.Length == 0 ? null : string.Join(' ', todo.Tags.Select(tag => $"#{tag}")),
            todo.Schedule is null ? null : todoRowRenderer.FormatSchedule(todo.Schedule)
        };
        var line = new System.Text.StringBuilder();
        themeRenderer.AppendStyled(line,
            $"LINK: {TaskLinkCode.Generate(assignment.Identity.ProjectPath, assignment.Identity.SourceLine)}  ·  ", theme.Info);
        themeRenderer.AppendStyled(line, todo.Title, theme.Heading, Decoration.Bold);
        if (view.SelectedSlot.Assignments.Length > 1)
        {
            themeRenderer.AppendStyled(
                line,
                $"  {view.SelectedSlot.Assignments.Length} STACKED · J/K SELECT",
                theme.Info,
                Decoration.Bold);
        }

        themeRenderer.AppendStyled(
            line,
            $"  {string.Join(" · ", metadata.Where(value => !string.IsNullOrEmpty(value)))}",
            theme.Muted,
            Decoration.Dim);
        return new Markup(line.ToString()).Ellipsis();
    }

    private IRenderable PlannerMultiDayCompactDetail(PlannerView view, TuiTheme theme)
    {
        var date = view.State.SelectedDate;
        var dateLabel = date.ToString("ddd dd").ToUpperInvariant();
        if (view.State.Focus == PlannerFocus.AllDay)
        {
            var item = view.SelectedAllDayItem;
            if (item is null)
            {
                return new Text($"{dateLabel} · ALL DAY · Empty destination",
                    themeRenderer.Style(theme.Muted, Decoration.Dim)).Ellipsis();
            }

            var estimate = item.Assignment?.Todo.Duration is { } duration
                ? $" · {todoRowRenderer.FormatDuration(duration)} estimate"
                : string.Empty;
            var source = item.Assignment is null ? "calendar" : "todo";
            return new Text($"{dateLabel} · ALL DAY{estimate} · {item.Title} ({source})",
                themeRenderer.Style(theme.Heading, Decoration.Bold)).Ellipsis();
        }

        if (view.SelectedItem is { } selected)
        {
            var source = selected.ItemType switch
            {
                PlannerItemType.Task => "todo",
                PlannerItemType.Pomodoro => "pomodoro",
                PlannerItemType.Meeting => "calendar",
                PlannerItemType.CalendarEvent => "calendar",
                PlannerItemType unknownItemType => throw new ArgumentOutOfRangeException(
                    nameof(selected.ItemType), unknownItemType, "Unsupported planner item type.")
            };
            var focus = selected.ItemType == PlannerItemType.Pomodoro ? view.ActiveFocusBlock : null;
            var duration = focus is null
                ? selected.Assignment?.Todo.Duration ?? selected.Duration
                : focus.EndsAt - focus.StartedAt;
            var start = focus is null ? selected.Start : TimeOnly.FromDateTime(focus.StartedAt);
            var startDate = focus is null ? date : DateOnly.FromDateTime(focus.StartedAt);
            var range = duration is { } span
                ? FormatMultiDayTimeRange(startDate, start, span)
                : start.ToString("HH:mm");
            var durationLabel = duration is { } timed
                ? $" · {(int)timed.TotalMinutes}m"
                : string.Empty;
            return new Text($"{dateLabel} · {range}{durationLabel} · {selected.Title} ({source})",
                themeRenderer.Style(selected.Meeting is null ? theme.Heading : theme.Info, Decoration.Bold)).Ellipsis();
        }

        return new Text($"{dateLabel} · {view.SelectedSlot.Time:HH:mm} · Empty destination",
            themeRenderer.Style(theme.Muted, Decoration.Dim)).Ellipsis();
    }

    private static string FormatMultiDayTimeRange(DateOnly date, TimeOnly start, TimeSpan duration)
    {
        var startAt = date.ToDateTime(start);
        var endAt = startAt.Add(duration);
        var endLabel = endAt.Date == startAt.Date
            ? endAt.ToString("HH:mm")
            : endAt.ToString("ddd dd HH:mm").ToUpperInvariant();
        return $"{start:HH:mm}–{endLabel}";
    }

    private static string FormatMoveDestination(PlannerView view, TimeSpan? duration)
    {
        var date = view.State.SelectedDate;
        if (view.State.Focus == PlannerFocus.AllDay)
        {
            return $"{date:yyyy-MM-dd} · ALL DAY";
        }

        var start = view.SelectedSlot.Time;
        return duration is { } timed
            ? $"{date:yyyy-MM-dd} {start:HH:mm}–{start.Add(timed):HH:mm}"
            : $"{date:yyyy-MM-dd} {start:HH:mm}";
    }

    private static string FormatCurrentSchedule(TodoSchedule? schedule, TimeSpan? duration)
    {
        if (schedule is null)
        {
            return "Unscheduled";
        }

        if (schedule.Time is null)
        {
            return $"{schedule.Date:yyyy-MM-dd} · ALL DAY";
        }

        var start = schedule.Time.Value;
        return duration is { } timed
            ? $"{schedule.Date:yyyy-MM-dd} {start:HH:mm}–{start.Add(timed):HH:mm}"
            : $"{schedule.Date:yyyy-MM-dd} {start:HH:mm}";
    }
}
