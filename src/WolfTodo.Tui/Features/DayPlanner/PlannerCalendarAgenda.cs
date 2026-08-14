using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerCalendarAgenda(
    ImmutableArray<PlannerCalendarAllDayItem> AllDayItems,
    ImmutableArray<PlannerCalendarMeeting> Meetings,
    PlannerCalendarSyncState SyncState,
    string? Error = null,
    string? Warning = null)
{
    public static PlannerCalendarAgenda Disabled { get; } = new([], [], PlannerCalendarSyncState.Disabled);

    public static PlannerCalendarAgenda Syncing { get; } = new([], [], PlannerCalendarSyncState.Syncing);
}
