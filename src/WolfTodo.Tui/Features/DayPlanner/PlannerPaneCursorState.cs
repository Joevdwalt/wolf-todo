namespace WolfTodo.Tui.Features.DayPlanner;

/// <summary>Transient cursor data independently retained by every date pane.</summary>
public sealed record PlannerPaneCursorState(
    int SlotIndex,
    string? SelectedTimelineItemIdentity,
    int AllDayIndex,
    PlannerFocus Focus)
{
    public static PlannerPaneCursorState Initial { get; } = new(0, null, 0, PlannerFocus.Timeline);
}
