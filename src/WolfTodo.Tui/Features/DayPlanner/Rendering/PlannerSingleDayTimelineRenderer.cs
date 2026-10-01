using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerSingleDayTimelineRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly PlannerTimelineLineRenderer lineRenderer;
    private readonly PlannerTimelineViewport viewport;

    public PlannerSingleDayTimelineRenderer(PlannerTimelineLineRenderer lineRenderer, PlannerTimelineViewport viewport)
    {
        this.lineRenderer = lineRenderer;
        this.viewport = viewport;
    }

    public Table CreatePlannerTimelineTable(
        IReadOnlyList<PlannerTimelineRow> timelineRows,
        int availableRows,
        TuiTheme theme)
    {
        var table = new Table().SquareBorder().Expand();
        table.BorderStyle = themeRenderer.Style(theme.BorderActive);
        table.AddColumn(new TableColumn(new Text(
            "TIME",
            themeRenderer.Style(theme.Heading, Decoration.Bold)))
        {
            Width = 8,
            NoWrap = true
        });
        table.AddColumn(new TableColumn(new Text("PLAN", themeRenderer.Style(theme.Accent, Decoration.Bold))));
        AddPlannerTimelineRows(table, timelineRows, theme);
        PadPlannerTimeline(table, timelineRows, availableRows);
        return table;
    }

    public IRenderable PlannerTimelineCell(
        IReadOnlyList<PlannerTimelineRenderRow> renderRows,
        int height,
        TuiTheme theme) =>
        new Rows(renderRows.Select(row =>
        {
            var content = lineRenderer.PlannerTimelineRenderLine(row, theme);
            return HighlightContent(row, content, theme);
        }).Concat(Enumerable.Repeat(PlannerTimelinePaddingLine(theme), height - renderRows.Count))
            .ToArray());

    private IRenderable PlannerTimelinePaddingLine(TuiTheme theme) =>
        new Text("│", themeRenderer.Style(theme.Muted, Decoration.Dim));

    public void AddPlannerTimelineRows(
        Table table,
        IReadOnlyList<PlannerTimelineRow> timelineRows,
        TuiTheme theme)
    {
        foreach (var row in timelineRows)
        {
            if (row is PlannerNowTimelineRow marker)
            {
                table.AddRow(
                    new Text(marker.Time.ToString("HH:mm").PadLeft(5), themeRenderer.Style(theme.Now, Decoration.Bold)),
                    new TimelineMarkerRenderable(
                        themeRenderer.Style(theme.Now, Decoration.Bold),
                        marker.TimeUntilNextMeeting,
                        marker.NextMeetingTitle,
                        themeRenderer.Style(theme.Timer, Decoration.Bold),
                        marker.PomodoroRemaining,
                        marker.PomodoroTitle));
                continue;
            }

            var slot = ((PlannerSlotTimelineRow)row).Slot;
            foreach (var renderRow in PlannerTimelineRenderModel.ForSlot(slot))
            {
                var time = lineRenderer.PlannerTimeRulerLine(renderRow, theme);
                var content = lineRenderer.PlannerTimelineRenderLine(renderRow, theme);
                var isSelectedEmptySlot = renderRow.IsEmpty && slot.IsSelected;
                table.AddRow(
                    isSelectedEmptySlot ? themeRenderer.OnSurface(time, theme.Surface2, true) : time,
                    HighlightContent(renderRow, content, theme));
            }
        }
    }

    public void PadPlannerTimeline(
        Table table,
        IReadOnlyList<PlannerTimelineRow> timelineRows,
        int availableRows)
    {
        for (var index = viewport.PlannerTimelineHeight(timelineRows); index < availableRows; index++)
        {
            table.AddEmptyRow();
        }
    }

    private IRenderable HighlightContent(PlannerTimelineRenderRow row, IRenderable content, TuiTheme theme)
    {
        if (!row.IsEmpty && (row.IsActive || row.IsSelected))
        {
            return themeRenderer.OnSurface(content, theme.Surface2);
        }

        if (row.IsEmpty && row.IsSelected)
        {
            return themeRenderer.OnSurface(content, theme.Surface2, true);
        }

        return content;
    }
}
