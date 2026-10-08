using System.Globalization;
using System.Text;
using Spectre.Console;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

/// <summary>Chooses the visible paths and text that fit in one date cell.</summary>
public sealed class PlannerMultiDayCellLayout
{
    private const int SeparatorWidth = 2;
    private const int MinimumSegmentWidth = 8;

    public PlannerMultiDayCellPlan Create(
        PlannerSlotView slot,
        IReadOnlyList<PlannerTimelineRenderRow> rows,
        DateOnly date,
        int contentWidth,
        PlannerFocusBlock? activeFocusBlock,
        IReadOnlyList<PlannerMultiDayLane>? lanes = null)
    {
        if (slot.Items.Length == 0)
        {
            return new PlannerMultiDayCellPlan(
                [CompactRow(rows[0], null, contentWidth, false, true)],
                [contentWidth], 0, true);
        }

        if (lanes is { Count: > 0 } &&
            CreateWithLanes(slot, rows, date, contentWidth, activeFocusBlock, lanes) is { } lanePlan)
        {
            return lanePlan;
        }

        // Original indices break ties without changing the presenter's order.
        var ordered = slot.Items.Select((item, index) => (Item: item, Row: rows[index], Index: index))
            .OrderBy(entry => entry.Item.Start)
            .ThenBy(entry => entry.Index)
            .ToArray();
        var capacity = Math.Max(1, (contentWidth + SeparatorWidth) /
                                    (MinimumSegmentWidth + SeparatorWidth));
        var visibleCount = ordered.Length <= capacity ? ordered.Length : Math.Max(1, capacity - 1);
        var selectedIndex = Array.FindIndex(ordered, entry => entry.Row.IsSelected || entry.Row.IsActive);
        var start = ordered.Length <= visibleCount || selectedIndex < 0
            ? 0
            : Math.Clamp(selectedIndex - visibleCount / 2, 0, ordered.Length - visibleCount);
        var visible = ordered.Skip(start).Take(visibleCount).ToArray();
        var hiddenCount = ordered.Length - visibleCount;
        var segmentCount = visibleCount + (hiddenCount > 0 ? 1 : 0);

        var availableWidth = Math.Max(segmentCount, contentWidth - (segmentCount - 1) * SeparatorWidth);

        var naturalWidths = visible.Select((entry, index) => NaturalWidth(
                entry.Row, entry.Item, ShowFinish(entry.Item, date, activeFocusBlock), index == 0))
            .Concat(hiddenCount > 0 ? [$" +{hiddenCount}".GetCellWidth()] : [])
            .ToArray();
        var widths = SegmentWidths(segmentCount, availableWidth, naturalWidths);
        var compactRows = visible.Select((entry, index) => CompactRow(
            entry.Row, entry.Item, widths[index],
            ShowFinish(entry.Item, date, activeFocusBlock), index == 0)).ToArray();

        return new PlannerMultiDayCellPlan(compactRows, widths, hiddenCount, false);
    }

    private static PlannerMultiDayCellPlan? CreateWithLanes(
        PlannerSlotView slot,
        IReadOnlyList<PlannerTimelineRenderRow> rows,
        DateOnly date,
        int contentWidth,
        PlannerFocusBlock? activeFocusBlock,
        IReadOnlyList<PlannerMultiDayLane> lanes)
    {
        var active = slot.Items.Select((item, index) => (Item: item, Row: rows[index]))
            .ToDictionary(entry => entry.Item.Identity);
        var lastActiveLane = -1;
        for (var index = 0; index < lanes.Count; index++)
        {
            if (active.ContainsKey(lanes[index].Identity))
            {
                lastActiveLane = index;
            }
        }

        if (lastActiveLane < 0 || active.Keys.Any(identity => lanes.All(lane => lane.Identity != identity)) ||
            lanes.Take(lastActiveLane + 1).Sum(lane => lane.Width) + lastActiveLane * SeparatorWidth > contentWidth)
        {
            return null;
        }

        var segments = new List<PlannerMultiDayCellSegment>();
        var visible = new List<PlannerTimelineRenderRow>();
        var gapWidth = 0;
        for (var index = 0; index <= lastActiveLane; index++)
        {
            var lane = lanes[index];
            if (!active.TryGetValue(lane.Identity, out var entry))
            {
                gapWidth += lane.Width + (gapWidth == 0 ? 0 : SeparatorWidth);
                continue;
            }

            if (gapWidth > 0)
            {
                segments.Add(new PlannerMultiDayCellSegment(null, gapWidth));
                gapWidth = 0;
            }

            var row = CompactRow(entry.Row, entry.Item, lane.Width,
                ShowFinish(entry.Item, date, activeFocusBlock), index == 0,
                lane.ReserveSelectionColumn);
            visible.Add(row);
            segments.Add(new PlannerMultiDayCellSegment(row, lane.Width));
        }

        return new PlannerMultiDayCellPlan(visible, segments.Select(segment => segment.Width).ToArray(),
            0, false, segments);
    }

    private static int[] SegmentWidths(int segmentCount, int availableWidth, int[] naturalWidths) =>
        segmentCount switch
        {
            1 => [availableWidth],
            int count when naturalWidths.Sum() <= availableWidth => naturalWidths,
            int count => Enumerable.Range(0, count)
                .Select(index => availableWidth / count + (index < availableWidth % count ? 1 : 0))
                .ToArray()
        };

    internal static bool ShowFinish(
        PlannerTimelineItemView item,
        DateOnly date,
        PlannerFocusBlock? activeFocusBlock) =>
        item.TimeShape == PlannerTimeShape.Duration &&
        item.IntervalState is (PlannerIntervalState.End or PlannerIntervalState.StartAndEnd) &&
        (item.ItemType != PlannerItemType.Pomodoro ||
         activeFocusBlock is null ||
         activeFocusBlock.EndsAt <= date.ToDateTime(new TimeOnly(22, 0)));

    internal static int NaturalWidth(
        PlannerTimelineRenderRow row,
        PlannerTimelineItemView item,
        bool showFinish,
        bool firstSegment)
    {
        var compact = CompactRow(row, item, int.MaxValue, showFinish, firstSegment);
        var prefixWidth = LanePrefix(row.IsSelected, firstSegment).GetCellWidth();
        // A continuation has no status glyph, but keeps the width of its
        // original label so the overlap separator stays in the same column.
        var statusWidth = row.StatusGlyph.Length == 0 ? 1 : row.StatusGlyph.GetCellWidth();
        var labelWidth = prefixWidth + 1 + statusWidth + 1 + item.Title.GetCellWidth();
        var finishWidth = item.TimeShape == PlannerTimeShape.Duration
            ? prefixWidth + $"{TodoGlyphs.PlannerFinishArrow}{TodoGlyphs.TreeContinuationGlyph}{item.End:HH:mm}".GetCellWidth()
            : 0;
        var renderedWidth = compact.BranchGlyph.GetCellWidth();
        if (compact.StatusGlyph.Length > 0)
        {
            renderedWidth += 1 + compact.StatusGlyph.GetCellWidth() + 1 + compact.Title.GetCellWidth();
            if (compact.Metadata.Length > 0)
            {
                renderedWidth += 1 + compact.Metadata.GetCellWidth();
            }
        }

        return Math.Max(renderedWidth, Math.Max(labelWidth, finishWidth));
    }

    private static PlannerTimelineRenderRow CompactRow(
        PlannerTimelineRenderRow row,
        PlannerTimelineItemView? item,
        int width,
        bool showFinish,
        bool firstSegment,
        bool reserveSelectionColumn = false)
    {
        if (row.IsEmpty)
        {
            return row with
            {
                BranchGlyph = row.IsSelected
                    ? $"{TodoGlyphs.PlannerSelectedPointer} {TodoGlyphs.TreeContinuationGlyph}"
                    : $"  {TodoGlyphs.TreeContinuationGlyph}",
                IsSelectionBridge = false
            };
        }

        var prefix = LanePrefix(row.IsSelected, firstSegment, reserveSelectionColumn);
        if (row.StatusGlyph.Length == 0)
        {
            var path = showFinish
                ? $"{TodoGlyphs.PlannerFinishArrow}{TodoGlyphs.TreeContinuationGlyph}{item!.End:HH:mm}"
                : TodoGlyphs.TreeContinuationGlyph;
            var branch = showFinish ? prefix + path : prefix + $" {TodoGlyphs.TreeContinuationGlyph}";
            return row with { BranchGlyph = branch, Title = string.Empty,
                Metadata = string.Empty, IsSelectionBridge = false };
        }

        var cue = showFinish
            ? $"{TodoGlyphs.PlannerFinishArrow}{TodoGlyphs.TreeContinuationGlyph}{item!.End:HH:mm}"
            : string.Empty;
        var fixedWidth = prefix.GetCellWidth() + 1 + row.StatusGlyph.GetCellWidth() + 1;
        var available = Math.Max(0, width - fixedWidth);
        var cueWidth = cue.GetCellWidth();
        if (cueWidth > available)
        {
            cue = string.Empty;
            cueWidth = 0;
        }

        // A title needs one separator before the cue. The cue can stand alone
        // when only its own width remains after the glyph.
        var remaining = available - cueWidth;
        var titleWidth = cueWidth == 0 ? available : Math.Max(0, remaining - 1);
        var title = TruncateTitle(row.Title, titleWidth);
        if (cueWidth > 0 && firstSegment && width != int.MaxValue)
        {
            if (titleWidth > 0)
            {
                title += new string(' ', Math.Max(0, titleWidth - title.GetCellWidth()));
            }
            else
            {
                cue = new string(' ', remaining) + cue;
            }
        }

        return row with { BranchGlyph = prefix, Title = title,
            Metadata = cue, IsSelectionBridge = false };
    }

    private static string LanePrefix(bool selected, bool firstSegment, bool reserveSelectionColumn = false) => selected switch
    {
        true when firstSegment => TodoGlyphs.PlannerSelectedPointer,
        true => $" {TodoGlyphs.PlannerSelectedPointer}",
        false when reserveSelectionColumn && !firstSegment => "  ",
        false => " "
    };

    private static string TruncateTitle(string title, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (title.GetCellWidth() <= width)
        {
            return title;
        }

        if (width == 1)
        {
            return "…";
        }

        var result = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(title);
        while (elements.MoveNext())
        {
            var next = elements.GetTextElement();
            if ((result.ToString() + next).GetCellWidth() > width - 1)
            {
                break;
            }

            result.Append(next);
        }

        return result.ToString().TrimEnd() + "…";
    }
}
