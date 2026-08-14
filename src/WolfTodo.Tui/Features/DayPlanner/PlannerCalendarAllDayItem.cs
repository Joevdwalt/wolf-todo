using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerCalendarAllDayItem(
    string Title,
    PlannerCalendarItemKind Kind,
    bool IsCompleted = false,
    TodoItem? Todo = null,
    string? ProjectTitle = null)
{
    public PlannerAssignment? Assignment { get; init; }

    public string? EventId { get; init; }

    public string? CalendarId { get; init; }

    public string? Location { get; init; }

    public ImmutableArray<string> Attendees { get; init; } = [];

    public string? Description { get; init; }

    public string Identity => Assignment is not null
        ? $"todo:{Assignment.Identity.ProjectPath}:{Assignment.Identity.SourceLine}"
        : $"calendar:{CalendarId ?? "primary"}:{EventId ?? $"{Kind}:{Title}"}";
}
