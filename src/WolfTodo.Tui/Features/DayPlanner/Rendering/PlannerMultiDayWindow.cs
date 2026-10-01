namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed record PlannerMultiDayWindow(
    IReadOnlyList<int> SlotIndices,
    int MarkerSlotIndex,
    bool IncludesNow);
