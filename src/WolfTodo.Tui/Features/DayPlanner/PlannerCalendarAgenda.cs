using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerCalendarAgenda(
    ImmutableArray<PlannerCalendarAllDayItem> AllDayItems,
    ImmutableArray<PlannerCalendarMeeting> Meetings,
    PlannerCalendarSyncState SyncState,
    string? Error = null)
{
    public static PlannerCalendarAgenda Disabled { get; } = new([], [], PlannerCalendarSyncState.Disabled);

    public static PlannerCalendarAgenda Syncing { get; } = new([], [], PlannerCalendarSyncState.Syncing);
}

public sealed record PlannerCalendarAllDayItem(
    string Title,
    PlannerCalendarItemKind Kind,
    bool IsCompleted = false,
    TodoItem? Todo = null,
    string? ProjectTitle = null);

public sealed record PlannerCalendarMeeting(string Title, TimeOnly Start, TimeOnly End)
{
    public string? EventId { get; init; }

    public string? Location { get; init; }

    public ImmutableArray<string> Attendees { get; init; } = [];

    public string? Description { get; init; }

    public string Identity => EventId ?? $"{Start:HH:mm}|{End:HH:mm}|{Title}";
}

public enum PlannerCalendarItemKind
{
    Event,
    FocusTime,
    OutOfOffice,
    Todo
}

public enum PlannerCalendarSyncState
{
    Disabled,
    Syncing,
    Ready,
    Offline,
    AuthenticationRequired,
    ConfigurationError
}
