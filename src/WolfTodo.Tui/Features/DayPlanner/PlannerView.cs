using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerView(
    PlannerState State,
    ImmutableArray<PlannerSlotView> Slots,
    ImmutableArray<PlannerAssignment> PickerTodos,
    ImmutableArray<PlannerProjectOption> Projects)
{
    public PlannerSlotView SelectedSlot => Slots[State.SlotIndex];

    public PlannerAssignment? SelectedPickerTodo =>
        PickerTodos.Length == 0 ? null : PickerTodos[Math.Clamp(State.PickerIndex, 0, PickerTodos.Length - 1)];
}
