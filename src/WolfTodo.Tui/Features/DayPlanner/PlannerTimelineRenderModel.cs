using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner;

public static class PlannerTimelineRenderModel
{
    public static IReadOnlyList<PlannerTimelineRenderRow> ForSlot(PlannerSlotView slot)
    {
        var (timeLabel, minorTick) = TimeRuler(slot.Time);
        if (slot.Items.Length > 0)
        {
            return slot.Items.Select((item, index) => ItemRow(
                item,
                index == 0 ? timeLabel : string.Empty,
                index == 0 && minorTick,
                Branch(item, index, slot.Items.Length))).ToArray();
        }

        return
        [
            new PlannerTimelineRenderRow(
                timeLabel, minorTick, minorTick ? TodoGlyphs.PlannerTimeTick : string.Empty,
                slot.IsSelected ? TodoGlyphs.PlannerSelectedBranch : TodoGlyphs.TreeContinuationGlyph,
                string.Empty, string.Empty, string.Empty,
                slot.IsSelected, false, false, null, null)
        ];
    }

    public static (string Label, bool IsMinorTick) TimeRuler(TimeOnly time) =>
        time.Minute is 0 or 30
            ? (time.ToString("HH:mm"), false)
            : (string.Empty, true);

    private static PlannerTimelineRenderRow ItemRow(
        PlannerTimelineItemView item,
        string timeLabel,
        bool minorTick,
        string branch)
    {
        var hasContent = item.IntervalState is not PlannerIntervalState.Continue and not PlannerIntervalState.End;
        var status = hasContent switch
        {
            true => StatusGlyph(item),
            false => string.Empty
        };
        var metadata = item.IntervalState == PlannerIntervalState.StartAndEnd && item.Duration is { } duration
            ? $"· {(int)duration.TotalMinutes}m"
            : string.Empty;
        return new PlannerTimelineRenderRow(
            timeLabel, minorTick, minorTick ? TodoGlyphs.PlannerTimeTick : string.Empty,
            branch, status, hasContent ? item.Title : string.Empty, metadata,
            item.IsSelected, item.IsActive, item.IsSelectionBridge, item.ItemType, item.IntervalState);
    }

    private static string StatusGlyph(PlannerTimelineItemView item) => item.ItemType switch
    {
        PlannerItemType.Task when item.IsCompleted => TodoGlyphs.CompletedTodoGlyph,
        PlannerItemType.Task => TodoGlyphs.PlannerOpenTaskStatus,
        PlannerItemType.Meeting => TodoGlyphs.PlannerMeetingStatus,
        PlannerItemType.CalendarEvent => TodoGlyphs.PlannerMeetingStatus,
        PlannerItemType.Pomodoro => TodoGlyphs.PomodoroGlyph,
        PlannerItemType unknownItemType => throw new ArgumentOutOfRangeException(
            nameof(item.ItemType), unknownItemType, "Unsupported planner item type.")
    };

    private static string Branch(PlannerTimelineItemView item, int index, int count)
    {
        if (item.IsSelected)
        {
            return TodoGlyphs.PlannerSelectedBranch;
        }

        var isLastOfSeveralItems = index == count - 1 && count > 1;
        var branch = item.IntervalState switch
        {
            PlannerIntervalState.Start => TodoGlyphs.TreeBranchGlyph,
            PlannerIntervalState.Continue => TodoGlyphs.TreeContinuationGlyph,
            PlannerIntervalState.End => TodoGlyphs.TreeEndBranchGlyph,
            PlannerIntervalState.Instant when isLastOfSeveralItems => TodoGlyphs.TreeEndBranchGlyph,
            PlannerIntervalState.Instant => TodoGlyphs.TreeBranchGlyph,
            PlannerIntervalState.StartAndEnd when isLastOfSeveralItems => TodoGlyphs.TreeEndBranchGlyph,
            PlannerIntervalState.StartAndEnd => TodoGlyphs.TreeBranchGlyph,
            PlannerIntervalState unknownIntervalState => throw new ArgumentOutOfRangeException(
                nameof(item.IntervalState), unknownIntervalState, "Unsupported planner interval state.")
        };

        // Keep a selected duration's bridged lane open, but only replace a
        // glyph that would otherwise close it. Starts and continuations retain
        // their ordinary ├─ and │ shapes.
        if (item.IsSelectionBridge && branch == TodoGlyphs.TreeEndBranchGlyph && index < count - 1)
        {
            return TodoGlyphs.TreeBranchGlyph;
        }

        return branch;
    }
}
