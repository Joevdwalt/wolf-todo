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
