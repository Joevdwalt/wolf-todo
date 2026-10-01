namespace WolfTodo.Tui.Features.DayPlanner;

/// <summary>
/// A physical terminal row compiled from one chronological Planner slot.
/// Every occupied row belongs to exactly one timeline item and uses the same
/// branch column, including duration continuations and overlapping starts.
/// </summary>
public sealed record PlannerTimelineRenderRow(
    string TimeLabel,
    bool IsMinorTimeTick,
    string TimeTickGlyph,
    string BranchGlyph,
    string StatusGlyph,
    string Title,
    string Metadata,
    bool IsSelected,
    bool IsActive,
    bool IsSelectionBridge,
    PlannerItemType? ItemType,
    PlannerIntervalState? IntervalState)
{
    public bool IsEmpty => ItemType is null;
}
