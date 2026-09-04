using System.Collections.Immutable;
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

    // Transient overview state. It changes presentation and navigation only;
    // Markdown schedules remain owned by individual todos.
    public PlannerViewMode ViewMode { get; init; } = PlannerViewMode.SingleDay;

    public int VisibleDayCount { get; init; } = 1;

    // The first date shown in the multiday timeline. Keeping this separate
    // from SelectedDate lets h/l move through visible columns before the
    // viewport itself needs to scroll.
    public DateOnly? VisibleStartDate { get; init; }

    public PlannerFocus Focus { get; init; } = PlannerFocus.Timeline;

    public int AllDayIndex { get; init; }

    public TodoIdentity? PendingAllDaySelection { get; init; }

    // A date pane owns its own cursor. This is deliberately session-only UI
    // state: schedules themselves remain in the Markdown todo files.
    public ImmutableDictionary<DateOnly, PlannerPaneCursorState> PaneCursors { get; init; } =
        ImmutableDictionary<DateOnly, PlannerPaneCursorState>.Empty;

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

    public PlannerState SaveActivePane() => this with
    {
        PaneCursors = PaneCursors.SetItem(
            SelectedDate,
            new PlannerPaneCursorState(SlotIndex, SelectedTimelineItemIdentity, AllDayIndex, Focus))
    };

    public PlannerState RestorePane(DateOnly date)
    {
        var cursor = PaneCursors.GetValueOrDefault(date, PlannerPaneCursorState.Initial);
        return this with
        {
            SelectedDate = date,
            SlotIndex = cursor.SlotIndex,
            SelectedTimelineItemIdentity = cursor.SelectedTimelineItemIdentity,
            AllDayIndex = cursor.AllDayIndex,
            Focus = cursor.Focus
        };
    }
}
