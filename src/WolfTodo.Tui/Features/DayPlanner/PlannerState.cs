using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerState(
    DateOnly SelectedDate,
    int SlotIndex,
    PlannerMode Mode,
    int PickerIndex,
    string FilterText,
    string FilterDraft,
    TodoIdentity? MovingTodo,
    string? Error)
{
    public bool ShowDetails { get; init; } = true;

    public PlannerFocus Focus { get; init; } = PlannerFocus.Timeline;

    public int AllDayIndex { get; init; }

    public TodoIdentity? PendingAllDaySelection { get; init; }

    // Identifies the task, meeting, calendar event, or focus block chosen from
    // an overlapping timeline slot. It is transient: the presenter falls back
    // to the slot's first stable item when this identity is absent.
    public string? SelectedTimelineItemIdentity { get; init; }

    public TodoTaskEditorState? Editor { get; init; }

    public static PlannerState CreateInitial(DateOnly today) => new(
        today,
        0,
        PlannerMode.Browse,
        0,
        string.Empty,
        string.Empty,
        null,
        null);

    public bool CapturesInput => Mode != PlannerMode.Browse || Editor is not null;
}
