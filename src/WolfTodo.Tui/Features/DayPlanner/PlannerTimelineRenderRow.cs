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
                timeLabel, minorTick, minorTick ? "—" : string.Empty,
                slot.IsSelected ? "├▶" : "│", string.Empty, string.Empty, string.Empty,
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
        var status = !hasContent
            ? string.Empty
            : item.ItemType switch
            {
                PlannerItemType.Task => item.IsCompleted ? "✓" : "○",
                PlannerItemType.Pomodoro => "◷",
                _ => "⬥"
            };
        var metadata = item.IntervalState == PlannerIntervalState.StartAndEnd && item.Duration is { } duration
            ? $"· {(int)duration.TotalMinutes}m"
            : string.Empty;
        return new PlannerTimelineRenderRow(
            timeLabel, minorTick, minorTick ? "—" : string.Empty,
            branch, status, hasContent ? item.Title : string.Empty, metadata,
            item.IsSelected, item.IsActive, item.IsSelectionBridge, item.ItemType, item.IntervalState);
    }

    private static string Branch(PlannerTimelineItemView item, int index, int count)
    {
        if (item.IsSelected)
        {
            return "├▶";
        }

        return item.IntervalState switch
        {
            PlannerIntervalState.Start => "├─",
            PlannerIntervalState.Continue => "│",
            PlannerIntervalState.End => "└─",
            _ => index == count - 1 && count > 1 ? "└─" : "├─"
        };
    }
}
