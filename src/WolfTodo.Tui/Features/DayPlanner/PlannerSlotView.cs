using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerSlotView(
    TimeOnly Time,
    ImmutableArray<PlannerAssignment> Assignments,
    bool IsSelected)
{
    public ImmutableArray<PlannerCalendarMeeting> Meetings { get; init; } = [];

    public DurationBlockPosition? DurationPosition { get; init; }

    public bool IsActiveAssignment { get; init; }
}

public enum DurationBlockPosition
{
    Start,
    Middle,
    End
}
