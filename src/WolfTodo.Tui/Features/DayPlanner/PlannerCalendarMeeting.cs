using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerCalendarMeeting(string Title, TimeOnly Start, TimeOnly End)
{
    public string? EventId { get; init; }

    public string? CalendarId { get; init; }

    public string? Location { get; init; }

    public ImmutableArray<string> Attendees { get; init; } = [];

    public string? Description { get; init; }

    public string Identity =>
        $"calendar:{CalendarId ?? "primary"}:{EventId ?? $"{Start:HH:mm}|{End:HH:mm}|{Title}"}";
}
