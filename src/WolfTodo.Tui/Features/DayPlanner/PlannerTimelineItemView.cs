using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public enum PlannerItemType
{
    Task,
    Meeting,
    CalendarEvent
}

public enum PlannerTimeShape
{
    Instant,
    Duration
}

public enum PlannerIntervalState
{
    Instant,
    Start,
    Continue,
    End,
    StartAndEnd
}

public sealed record PlannerTimelineItemView(
    PlannerItemType ItemType,
    string Identity,
    string Title,
    TimeOnly Start,
    TimeOnly End,
    PlannerTimeShape TimeShape,
    PlannerIntervalState IntervalState,
    bool IsCompleted,
    bool IsSelected,
    PlannerAssignment? Assignment = null,
    PlannerCalendarMeeting? Meeting = null,
    // Active identity is intentionally independent from the cursor. A timed
    // item stays highlighted across its interval, while its cursor stays on
    // the one slot the user is currently navigating.
    bool IsActive = false)
{
    public TimeSpan? Duration => TimeShape == PlannerTimeShape.Duration ? End - Start : null;
}
