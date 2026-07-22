using System.Collections.Immutable;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerView(
    PlannerState State,
    ImmutableArray<PlannerSlotView> Slots,
    ImmutableArray<PlannerAssignment> PickerTodos,
    ImmutableArray<PlannerProjectOption> Projects)
{
    public PlannerCalendarAgenda CalendarAgenda { get; init; } = PlannerCalendarAgenda.Disabled;

    public string? GlobalCommand { get; init; }

    public string? GlobalError { get; init; }

    public CommandPaletteView? CommandPalette { get; init; }

    public int OpenTodoCount { get; init; }

    public int ProjectErrorCount { get; init; }

    public PlannerSlotView SelectedSlot => Slots[State.SlotIndex];

    public PlannerAssignment? SelectedPickerTodo =>
        PickerTodos.Length == 0 ? null : PickerTodos[Math.Clamp(State.PickerIndex, 0, PickerTodos.Length - 1)];

    // The timeline deliberately selects its first stable branch when items overlap.
    public PlannerTimelineItemView? SelectedItem =>
        SelectedSlot.Items.FirstOrDefault(item => item.IsSelected) ??
        SelectedSlot.Items.FirstOrDefault();

    public PlannerAssignment? SelectedAssignment => SelectedItem?.Assignment;

    public PlannerCalendarMeeting? SelectedMeeting => SelectedItem?.Meeting;
}
