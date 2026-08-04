using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class CalendarItemRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly TodoRowRenderer todoRowRenderer = new();

    public string MeetingHint(PlannerSlotView slot) =>
        slot.Meetings.Length == 0 ? string.Empty : $"  ⚠ {MeetingLabel(slot.Meetings[0])}" +
            (slot.Meetings.Length > 1 ? $" +{slot.Meetings.Length - 1}" : string.Empty);

    public string MeetingLabel(PlannerCalendarMeeting meeting) =>
        $"{meeting.Start:HH:mm}–{meeting.End:HH:mm} {meeting.Title}";

    public IReadOnlyList<IRenderable> MeetingDetailLines(PlannerView view, TuiTheme theme)
    {
        var meeting = view.SelectedMeeting!;
        var lines = new List<IRenderable>
        {
            new Text(meeting.Title, themeRenderer.Style(theme.Info, Decoration.Bold))
        };
        AddField(lines, "Time", MeetingTimeAndDuration(meeting), theme, theme.Date);
        AddField(lines, "Location", meeting.Location, theme, theme.Text);
        AddField(
            lines,
            "Attendees",
            meeting.Attendees.Length == 0 ? null : string.Join(", ", meeting.Attendees),
            theme,
            theme.SecondaryText);
        AddField(lines, "Notes", MeetingDescriptionPreview(meeting.Description), theme, theme.Text);
        if (view.SelectedSlot.Meetings.Length > 1)
        {
            AddField(
                lines,
                "Also",
                string.Join(" · ", view.SelectedSlot.Meetings
                    .Where(candidate => candidate.Identity != meeting.Identity)
                    .Select(MeetingLabel)),
                theme,
                theme.Warning);
        }

        return lines;
    }

    public string MeetingTimeAndDuration(PlannerCalendarMeeting meeting) =>
        $"{meeting.Start:HH:mm}–{meeting.End:HH:mm} · {(int)(meeting.End - meeting.Start).TotalMinutes}m";

    public string? MeetingDescriptionPreview(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = string.Join(' ', description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 120 ? normalized : normalized[..117] + "…";
    }

    public string AllDayKindLabel(PlannerCalendarItemKind kind) => kind switch
    {
        PlannerCalendarItemKind.FocusTime => "Focus time",
        PlannerCalendarItemKind.OutOfOffice => "Out of office",
        PlannerCalendarItemKind.Todo => "Todo",
        _ => "Calendar event"
    };

    public IReadOnlyList<IRenderable> AllDayDetailLines(PlannerView view, TuiTheme theme)
    {
        var item = view.SelectedAllDayItem;
        if (item is null)
        {
            return
            [
                new Text("EMPTY ALL DAY", themeRenderer.Style(theme.Muted, Decoration.Dim)),
                new Text("Press Enter to assign an existing todo or a to create one.", themeRenderer.Style(theme.Muted))
            ];
        }

        if (item.Assignment is { } assignment)
        {
            var todo = assignment.Todo;
            var lines = new List<IRenderable>
            {
                new Text(todo.Title, themeRenderer.Style(theme.Heading, Decoration.Bold))
            };
            AddField(lines, "Project", assignment.ProjectTitle, theme, theme.Text);
            AddField(lines, "Scheduled", $"{view.State.SelectedDate:yyyy-MM-dd} · ALL DAY", theme, theme.Date);
            AddField(lines, "Reference", todo.ExternalReference, theme, theme.Info);
            AddField(lines, "Priority", todo.Priority?.ToString(), theme, todoRowRenderer.PriorityColor(todo.Priority, theme));
            AddField(
                lines,
                "Tags",
                todo.Tags.Length == 0 ? null : string.Join(", ", todo.Tags.Select(tag => $"#{tag}")),
                theme,
                theme.Tag);
            AddField(lines, "Duration", todoRowRenderer.FormatDuration(todo.Duration), theme, theme.Info);
            return lines;
        }

        var calendarLines = new List<IRenderable>
        {
            new Text(item.Title, themeRenderer.Style(theme.Info, Decoration.Bold))
        };
        AddField(calendarLines, "Type", AllDayKindLabel(item.Kind), theme, theme.Info);
        AddField(calendarLines, "Scheduled", $"{view.State.SelectedDate:yyyy-MM-dd} · ALL DAY", theme, theme.Date);
        AddField(calendarLines, "Location", item.Location, theme, theme.Text);
        AddField(
            calendarLines,
            "Attendees",
            item.Attendees.Length == 0 ? null : string.Join(", ", item.Attendees),
            theme,
            theme.SecondaryText);
        AddField(calendarLines, "Notes", MeetingDescriptionPreview(item.Description), theme, theme.Text);
        calendarLines.Add(new Text("READ-ONLY CALENDAR ITEM", themeRenderer.Style(theme.Muted, Decoration.Dim)));
        return calendarLines;
    }

    public IReadOnlyList<IRenderable> AllDayAgendaLines(
        PlannerView view,
        TuiTheme theme,
        int contentHeight)
    {
        if (view.CalendarAgenda.AllDayItems.Length == 0)
        {
            return view.State.Focus == PlannerFocus.AllDay
                ? [new Text("> — ADD ALL-DAY TASK", themeRenderer.Style(theme.AccentBright, Decoration.Bold))]
                : [new Text("NO ALL-DAY ITEMS", themeRenderer.Style(theme.Muted, Decoration.Dim))];
        }

        var lines = view.CalendarAgenda.AllDayItems.Select((item, index) =>
        {
            var selected = view.State.Focus == PlannerFocus.AllDay && index == view.State.AllDayIndex;
            if (item.Todo is not null)
            {
                return CalendarTodoLine(
                    item.Todo,
                    item.ProjectTitle,
                    selected ? ">" : " ",
                    selected,
                    string.Empty,
                    theme);
            }

            var color = selected ? theme.AccentBright : item.IsCompleted ? theme.Muted :
                item.Kind == PlannerCalendarItemKind.Todo ? theme.Text : theme.Info;
            return (IRenderable)new Text($"{(selected ? ">" : " ")} ◆ {item.Title}", themeRenderer.Style(
                color,
                selected ? Decoration.Bold : item.IsCompleted ? Decoration.Dim : Decoration.None)).Ellipsis();
        }).ToArray();
        return FitLines(lines, contentHeight, view.State.AllDayIndex);
    }

    public IRenderable CalendarTodoLine(
        TodoItem todo,
        string? projectTitle,
        string prefix,
        bool selected,
        string meetingHint,
        TuiTheme theme)
    {
        var completed = todo.IsCompleted;
        var decoration = selected ? Decoration.Bold : completed ? Decoration.Dim : Decoration.None;
        var baseColor = selected ? theme.AccentBright : completed ? theme.Muted : theme.Text;
        var markerColor = selected ? theme.AccentBright : completed ? theme.Muted : theme.Accent;
        var priorityColor = selected ? theme.AccentBright : completed ? theme.Muted : todoRowRenderer.PriorityColor(todo.Priority, theme);
        var reference = todo.ExternalReference is null ? string.Empty : $"{todo.ExternalReference} - ";
        var tags = todo.Tags.Length == 0 ? string.Empty : $" {string.Join(' ', todo.Tags.Select(tag => $"#{tag}"))}";
        var schedule = todo.Schedule is null ? string.Empty : $" ⏳ {todoRowRenderer.FormatSchedule(todo.Schedule)}";
        var line = new System.Text.StringBuilder();

        themeRenderer.AppendStyled(line, $"{prefix} ", baseColor, decoration);
        themeRenderer.AppendStyled(line, todoRowRenderer.StatusGlyph(completed), markerColor, decoration);
        themeRenderer.AppendStyled(line, " ", baseColor, decoration);
        themeRenderer.AppendStyled(line, todoRowRenderer.PriorityCode(todo.Priority), priorityColor, decoration);
        themeRenderer.AppendStyled(line, " ", baseColor, decoration);
        themeRenderer.AppendStyled(line, reference, selected ? theme.AccentBright : completed ? theme.Muted : theme.Info, decoration);
        themeRenderer.AppendStyled(line, todo.Title, baseColor, decoration);
        themeRenderer.AppendStyled(
            line,
            projectTitle is null ? string.Empty : $"  [{projectTitle}]",
            selected ? theme.AccentBright : completed ? theme.Muted : theme.SecondaryText,
            decoration);
        themeRenderer.AppendStyled(line, tags, selected ? theme.AccentBright : completed ? theme.Muted : theme.Tag, decoration);
        themeRenderer.AppendStyled(line, schedule, selected ? theme.AccentBright : completed ? theme.Muted : theme.Date, decoration);
        themeRenderer.AppendStyled(line, meetingHint, theme.Warning, decoration);
        return new Markup(line.ToString());
    }

    public IReadOnlyList<IRenderable> FitLines(
        IReadOnlyList<IRenderable> lines,
        int contentHeight,
        int selectedIndex)
    {
        if (lines.Count <= contentHeight)
        {
            return lines;
        }

        var start = Math.Clamp(selectedIndex - contentHeight + 1, 0, lines.Count - contentHeight);
        return lines.Skip(start).Take(contentHeight).ToArray();
    }

    public void AddField(
        List<IRenderable> lines,
        string name,
        string? value,
        TuiTheme theme,
        Color valueColor)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var line = new System.Text.StringBuilder();
        themeRenderer.AppendStyled(line, $"{name.ToUpperInvariant()}: ", theme.Heading, Decoration.Bold);
        themeRenderer.AppendStyled(line, value, valueColor);
        lines.Add(new Markup(line.ToString()));
    }
}
