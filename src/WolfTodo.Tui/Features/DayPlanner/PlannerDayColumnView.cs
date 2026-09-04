using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.DayPlanner;

/// <summary>
/// The independently presented data for one date in the multiday timeline.
/// The active date is the only column that owns planner selection state.
/// </summary>
public sealed record PlannerDayColumnView(
    DateOnly Date,
    ImmutableArray<PlannerSlotView> Slots,
    PlannerCalendarAgenda CalendarAgenda,
    bool IsActive);
