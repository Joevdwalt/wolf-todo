namespace WolfTodo.Tui.Features.DayPlanner;

/// <summary>
/// A physical terminal row compiled from one chronological Planner slot.
/// The primary branch is the ordinary timeline tree; SecondaryPrefix is used
/// only while another item overlaps a continuing duration interval.
/// </summary>
public sealed record PlannerTimelineRenderRow(
    string TimeLabel,
    bool IsMinorTimeTick,
    string TimeTickGlyph,
    string PrimaryBranchGlyph,
    string SecondaryPrefix,
    string ActivityBranchGlyph,
    string StatusGlyph,
    string Title,
    string Metadata,
    bool IsSelected,
    bool IsPrimarySelected,
    bool IsActive,
    bool IsPrimaryActive,
    PlannerItemType? ItemType,
    PlannerIntervalState? IntervalState)
{
    public bool IsEmpty => ItemType is null;

    public bool UsesSecondaryLane => SecondaryPrefix.Length > 0;
}

public static class PlannerTimelineRenderModel
{
    public static IReadOnlyList<PlannerTimelineRenderRow> ForSlot(PlannerSlotView slot)
    {
        var (timeLabel, minorTick) = TimeRuler(slot.Time);
        var continuing = slot.Items
            .Where(item => item.IntervalState == PlannerIntervalState.Continue)
            .ToArray();
        var starting = slot.Items
            .Where(item => item.IntervalState is PlannerIntervalState.Instant or PlannerIntervalState.Start or PlannerIntervalState.StartAndEnd)
            .ToArray();
        var ending = slot.Items
            .Where(item => item.IntervalState == PlannerIntervalState.End)
            .ToArray();

        // A continuing interval only receives a second lane when another item
        // genuinely starts in the same slot. Extra continuations remain stacked
        // in that lane deterministically rather than allocating more columns.
        if (continuing.Length > 0 && starting.Length > 0)
        {
            var rows = new List<PlannerTimelineRenderRow>();
            var overlapItems = starting.Concat(continuing.Skip(1)).ToArray();
            for (var index = 0; index < overlapItems.Length; index++)
            {
                rows.Add(ItemRow(
                    overlapItems[index],
                    index == 0 ? timeLabel : string.Empty,
                    index == 0 && minorTick,
                    SelectedPrimaryBranch(continuing[0]),
                    continuing[0].IsSelected,
                    continuing[0].IsActive,
                    string.Empty,
                    GroupBranch(overlapItems[index], index, overlapItems.Length)));
            }

            return rows;
        }

        if (starting.Length > 0)
        {
            return starting.Select((item, index) => ItemRow(
                item,
                index == 0 ? timeLabel : string.Empty,
                index == 0 && minorTick,
                string.Empty,
                false,
                false,
                string.Empty,
                GroupBranch(item, index, starting.Length))).ToArray();
        }

        if (continuing.Length > 0)
        {
            return continuing.Select((item, index) => ItemRow(
                item,
                index == 0 ? timeLabel : string.Empty,
                index == 0 && minorTick,
                index == 0 ? SelectedPrimaryBranch(item) : "│",
                index == 0 && item.IsSelected,
                index == 0 && item.IsActive,
                string.Empty,
                string.Empty)).ToArray();
        }

        if (ending.Length > 0)
        {
            return ending.Select((item, index) => ItemRow(
                item,
                index == 0 ? timeLabel : string.Empty,
                index == 0 && minorTick,
                index == 0 ? SelectedPrimaryBranch(item, "└─") : "│",
                index == 0 && item.IsSelected,
                index == 0 && item.IsActive,
                string.Empty,
                string.Empty)).ToArray();
        }

        return
        [
            new PlannerTimelineRenderRow(
                timeLabel, minorTick, minorTick ? "—" : string.Empty,
                slot.IsSelected ? "├▶" : "│", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                slot.IsSelected, slot.IsSelected, false, false, null, null)
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
        string primaryBranch,
        bool isPrimarySelected,
        bool isPrimaryActive,
        string secondaryPrefix,
        string activityBranch)
    {
        var hasContent = item.IntervalState is not PlannerIntervalState.Continue and not PlannerIntervalState.End;
        var status = !hasContent
            ? string.Empty
            : item.ItemType == PlannerItemType.Task
                ? item.IsCompleted ? "✓" : "○"
                : "◆";
        var metadata = item.IntervalState == PlannerIntervalState.StartAndEnd && item.Duration is { } duration
            ? $"· {(int)duration.TotalMinutes}m"
            : string.Empty;
        return new PlannerTimelineRenderRow(
            timeLabel, minorTick, minorTick ? "—" : string.Empty,
            primaryBranch, secondaryPrefix, activityBranch, status,
            hasContent ? item.Title : string.Empty, metadata,
            item.IsSelected, isPrimarySelected, item.IsActive, isPrimaryActive, item.ItemType, item.IntervalState);
    }

    private static string SelectedPrimaryBranch(PlannerTimelineItemView item, string glyph = "│") =>
        item.IsSelected ? "├▶" : glyph;

    private static string GroupBranch(PlannerTimelineItemView item, int index, int count)
    {
        if (item.IsSelected)
        {
            return "├▶";
        }

        return index == count - 1 && count > 1 ? "└─" : "├─";
    }
}
