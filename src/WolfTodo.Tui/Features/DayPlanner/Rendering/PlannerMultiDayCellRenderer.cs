using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerMultiDayCellRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly PlannerMultiDayCellLayout layout = new();
    private readonly PlannerTimelineLineRenderer lineRenderer;

    public PlannerMultiDayCellRenderer(PlannerTimelineLineRenderer lineRenderer)
    {
        this.lineRenderer = lineRenderer;
    }

    // The layout decides what fits; this method turns that plan into styled columns.
    public IRenderable PlannerMultiDayTimelineCell(
        PlannerSlotView slot,
        IReadOnlyList<PlannerTimelineRenderRow> rows,
        DateOnly date,
        int contentWidth,
        TuiTheme theme,
        PlannerFocusBlock? activeFocusBlock,
        IReadOnlyList<PlannerMultiDayLane>? lanes = null)
    {
        var plan = layout.Create(slot, rows, date, contentWidth, activeFocusBlock, lanes);
        if (plan.IsEmptySlot)
        {
            return RenderEmptySlot(plan.VisibleRows[0], theme);
        }

        if (plan.LaneSegments is { } laneSegments)
        {
            var laneEntries = laneSegments.Select(segment => segment.Row is { } row
                ? RenderItem(row, theme)
                : new Text($"  {TodoGlyphs.TreeContinuationGlyph}", themeRenderer.Style(theme.Muted, Decoration.Dim))).ToArray();
            return BuildCellTable(laneEntries, plan.SegmentWidths, theme);
        }

        var entries = plan.VisibleRows.Select(row => RenderItem(row, theme)).ToList();
        if (plan.HiddenCount > 0)
        {
            entries.Add(new Text($" +{plan.HiddenCount}", themeRenderer.Style(theme.Warning, Decoration.Bold)));
        }

        return BuildCellTable(entries, plan.SegmentWidths, theme);
    }

    private IRenderable RenderEmptySlot(PlannerTimelineRenderRow row, TuiTheme theme) =>
        row.IsSelected
            ? lineRenderer.PlannerTimelineRenderLine(row, theme)
            : new Text($"  {TodoGlyphs.TreeContinuationGlyph}", themeRenderer.Style(theme.Muted, Decoration.Dim));

    // Continuation rows contain only a path; starts carry a status glyph and title.
    private IRenderable RenderItem(PlannerTimelineRenderRow row, TuiTheme theme)
    {
        var line = row.StatusGlyph.Length == 0
            ? PlannerMultiDayPathLine(row, theme)
            : lineRenderer.PlannerTimelineRenderLine(row, theme);
        return row.IsActive || row.IsSelected
            ? themeRenderer.OnSurface(line, theme.Surface2)
            : line;
    }

    // Each overlap owns a fixed-width column so Spectre cannot wrap the slot.
    private IRenderable BuildCellTable(IReadOnlyList<IRenderable> entries, IReadOnlyList<int> widths, TuiTheme theme)
    {
        const int separatorWidth = 2;
        var cell = new Table().NoBorder().Collapse().HideHeaders();
        var cellContents = new List<IRenderable>();
        for (var index = 0; index < entries.Count; index++)
        {
            cell.AddColumn(new TableColumn(entries[index])
            {
                Width = widths[index],
                NoWrap = true,
                Padding = new Padding(0, 0)
            });
            cellContents.Add(entries[index]);
            if (index < entries.Count - 1)
            {
                var separator = new Text(" ┊", themeRenderer.Style(theme.Muted, Decoration.Dim));
                cell.AddColumn(new TableColumn(separator)
                {
                    Width = separatorWidth,
                    NoWrap = true,
                    Padding = new Padding(0, 0)
                });
                cellContents.Add(separator);
            }
        }

        cell.AddRow(cellContents.ToArray());
        return cell;
    }

    private IRenderable PlannerMultiDayPathLine(PlannerTimelineRenderRow row, TuiTheme theme)
    {
        var isHighlighted = row.IsActive || row.IsSelected;
        var color = isHighlighted switch
        {
            true => theme.AccentBright,
            false => row.ItemType switch
            {
                PlannerItemType.Task => theme.BorderActive,
                PlannerItemType.Meeting => theme.BorderActive,
                PlannerItemType.CalendarEvent => theme.BorderActive,
                PlannerItemType.Pomodoro => theme.Timer,
                null => theme.BorderActive,
                PlannerItemType unknownItemType => throw new ArgumentOutOfRangeException(
                    nameof(row.ItemType), unknownItemType, "Unsupported planner item type.")
            }
        };
        var decoration = isHighlighted switch
        {
            true => Decoration.Bold,
            false => Decoration.None
        };
        return new Text(row.BranchGlyph, themeRenderer.Style(color, decoration)).Ellipsis();
    }
}
