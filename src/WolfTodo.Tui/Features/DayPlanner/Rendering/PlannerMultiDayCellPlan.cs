using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed record PlannerMultiDayCellPlan(
    IReadOnlyList<PlannerTimelineRenderRow> VisibleRows,
    IReadOnlyList<int> SegmentWidths,
    int HiddenCount,
    bool IsEmptySlot,
    IReadOnlyList<PlannerMultiDayCellSegment>? LaneSegments = null);
