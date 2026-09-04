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

    public string? TimerStatus { get; init; }

    public bool TimerIsBright { get; init; }

    public PomodoroPromptState? PomodoroPrompt { get; init; }

    public PomodoroCompletion? PomodoroCompletion { get; init; }

    public PlannerFocusBlock? ActiveFocusBlock { get; init; }

    public int OpenTodoCount { get; init; }

    public int ProjectErrorCount { get; init; }

    public PlannerSlotView SelectedSlot => Slots[State.SlotIndex];

    public PlannerAssignment? SelectedPickerTodo =>
        PickerTodos.Length == 0 ? null : PickerTodos[Math.Clamp(State.PickerIndex, 0, PickerTodos.Length - 1)];

    // The timeline deliberately selects its first stable item when no explicit
    // overlapping-item identity is available.
    public PlannerTimelineItemView? SelectedItem =>
        SelectedSlot.Items.FirstOrDefault(item => item.IsSelected) ??
        SelectedSlot.Items.FirstOrDefault();

    public PlannerAssignment? SelectedAssignment => SelectedItem?.Assignment;

    public PlannerCalendarAllDayItem? SelectedAllDayItem => CalendarAgenda.AllDayItems.Length == 0
        ? null
        : CalendarAgenda.AllDayItems[Math.Clamp(State.AllDayIndex, 0, CalendarAgenda.AllDayItems.Length - 1)];

    public PlannerAssignment? SelectedAllDayAssignment => SelectedAllDayItem?.Assignment;

    public PlannerAssignment? SelectedFocusedAssignment => State.Focus == PlannerFocus.AllDay
        ? SelectedAllDayAssignment
        : SelectedAssignment;

    public PlannerCalendarMeeting? SelectedMeeting => SelectedItem?.Meeting;
}
