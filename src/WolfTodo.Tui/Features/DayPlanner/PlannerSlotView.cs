using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.DayPlanner;

// Compatibility projection retained while callers migrate to Items.
public enum DurationBlockPosition { Start, Middle, End }

public sealed record PlannerSlotView(
    TimeOnly Time,
    ImmutableArray<PlannerAssignment> Assignments,
    bool IsSelected)
{
    public ImmutableArray<PlannerTimelineItemView> Items { get; init; } = [];

    public ImmutableArray<PlannerCalendarMeeting> Meetings { get; init; } = [];

    public PlannerCalendarMeeting? PrimaryMeeting => Meetings.FirstOrDefault();

    public DurationBlockPosition? DurationPosition =>
        ToBlockPosition(Items.FirstOrDefault(item => item.Assignment is not null)?.IntervalState);

    public DurationBlockPosition? MeetingDurationPosition =>
        ToBlockPosition(Items.FirstOrDefault(item => item.Meeting is not null)?.IntervalState);

    public bool IsActiveAssignment => Items.Any(item => item.Assignment is not null && item.IsActive);

    public bool IsActiveMeeting => Items.Any(item => item.Meeting is not null && item.IsActive);

    private static DurationBlockPosition? ToBlockPosition(PlannerIntervalState? state) => state switch
    {
        PlannerIntervalState.Start => DurationBlockPosition.Start,
        PlannerIntervalState.Continue => DurationBlockPosition.Middle,
        PlannerIntervalState.End => DurationBlockPosition.End,
        _ => null
    };
}
