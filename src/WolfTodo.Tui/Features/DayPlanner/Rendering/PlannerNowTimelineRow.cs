namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed record PlannerNowTimelineRow(
    TimeOnly Time,
    TimeSpan? TimeUntilNextMeeting = null,
    string? NextMeetingTitle = null) : PlannerTimelineRow;
