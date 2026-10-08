using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerMultiDayTimelineRenderer
{
    private readonly Func<int> widthProvider;
    private readonly Func<DateTime> nowProvider;
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly CalendarItemRenderer calendarItemRenderer = new();
    private readonly PlannerTimelineLineRenderer lineRenderer;
    private readonly PlannerTimelineViewport viewport;
    private readonly PlannerMultiDayCellRenderer cellRenderer;
    private readonly PlannerMultiDayLaneLayout laneLayout = new();

    public PlannerMultiDayTimelineRenderer(
        Func<int> widthProvider,
        Func<DateTime> nowProvider,
        PlannerTimelineLineRenderer lineRenderer,
        PlannerTimelineViewport viewport,
        PlannerMultiDayCellRenderer cellRenderer)
    {
        this.widthProvider = widthProvider;
        this.nowProvider = nowProvider;
        this.lineRenderer = lineRenderer;
        this.viewport = viewport;
        this.cellRenderer = cellRenderer;
    }

    public Table CreatePlannerMultiDayTimelineTable(
        IReadOnlyList<PlannerDayColumnView> columns,
        int selectedSlotIndex,
        int availableRows,
        TuiTheme theme) =>
        CreatePlannerMultiDayTimelineTable(columns, selectedSlotIndex, availableRows, theme, null);

    public Table CreatePlannerMultiDayTimelineTable(
        PlannerView view,
        int availableRows,
        TuiTheme theme) =>
        CreatePlannerMultiDayTimelineTable(
            view.DayColumns,
            view.State.SlotIndex,
            availableRows,
            theme,
            view);

    private Table CreatePlannerMultiDayTimelineTable(
        IReadOnlyList<PlannerDayColumnView> columns,
        int selectedSlotIndex,
        int availableRows,
        TuiTheme theme,
        PlannerView? view)
    {
        // Reserve the fixed ruler, borders, and separators before selecting
        // panes. The active pane is always retained when the terminal can
        // show fewer dates than the selected range.
        var terminalWidth = widthProvider();
        // TIME occupies 10 cells. Reserve borders and two padding cells per
        // date before enforcing its 24-cell content minimum.
        var maxColumns = Math.Max(1, (terminalWidth - 12) / 27);
        if (columns.Count > maxColumns)
        {
            var activeIndex = Math.Max(0, columns.ToList().FindIndex(column => column.IsActive));
            var start = Math.Clamp(activeIndex - maxColumns + 1, 0, columns.Count - maxColumns);
            columns = columns.Skip(start).Take(maxColumns).ToArray();
        }

        var table = new Table().SquareBorder().Expand();
        table.BorderStyle = themeRenderer.Style(theme.BorderActive);
        table.AddColumn(new TableColumn(new Text(
            "TIME",
            themeRenderer.Style(theme.Heading, Decoration.Bold)))
        {
            Width = 8,
            NoWrap = true,
            Padding = new Padding(1, 0)
        });
        var paneWidth = Math.Max(24, (terminalWidth - 12 - columns.Count) / columns.Count - 2);
        foreach (var column in columns)
        {
            var heading = (column.IsActive ? "▶ " : "  ") + column.Date.ToString("ddd dd").ToUpperInvariant();
            table.AddColumn(new TableColumn(new Text(
                heading,
                themeRenderer.Style(column.IsActive ? theme.AccentBright : theme.Accent, Decoration.Bold)))
            {
                Width = paneWidth,
                NoWrap = true,
                Padding = new Padding(1, 0)
            });
        }

        var now = nowProvider();
        var today = DateOnly.FromDateTime(now);
        var currentTime = new TimeOnly(now.Hour, now.Minute);
        var window = viewport.WindowPlannerMultiDayTimeline(columns, selectedSlotIndex, availableRows, now);
        var laneContexts = columns.Select(column => laneLayout.Create(
            column.Slots, column.Date, view?.ActiveFocusBlock)).ToArray();
        foreach (var slotIndex in window.SlotIndices)
        {
            if (window.IncludesNow && window.MarkerSlotIndex == slotIndex)
            {
                AddPlannerMultiDayNowRow(table, columns, today, currentTime, now, theme, view);
            }

            var slotRows = columns
                .Select(column => PlannerTimelineRenderModel.ForSlot(column.Slots[slotIndex]))
                .ToArray();
            var activeRows = slotRows.FirstOrDefault(rows => rows.Any(renderRow => renderRow.IsSelected));
            var timeCell = lineRenderer.PlannerTimeRulerLine(
                activeRows?[0] ?? slotRows[0][0],
                theme,
                selected: activeRows is not null);
            var cells = new List<IRenderable> { timeCell };
            cells.AddRange(columns.Select((column, index) => cellRenderer.PlannerMultiDayTimelineCell(
                column.Slots[slotIndex], slotRows[index], column.Date, paneWidth, theme,
                view?.ActiveFocusBlock, laneContexts[index].GetValueOrDefault(slotIndex))));
            table.AddRow(cells.ToArray());
        }

        if (window.IncludesNow && window.MarkerSlotIndex > window.SlotIndices[^1])
        {
            AddPlannerMultiDayNowRow(table, columns, today, currentTime, now, theme, view);
        }

        if (view is not null)
        {
            var allDayHeight = Math.Max(1, Math.Min(4, columns.Max(column => column.CalendarAgenda.AllDayItems.Length)));
            var allDayCells = new List<IRenderable>
            {
                new Text("ALL DAY", themeRenderer.Style(theme.Heading, Decoration.Bold))
            };
            allDayCells.AddRange(columns.Select(column => PlannerDayAllDayCell(view, column, allDayHeight, theme)));
            table.AddRow(allDayCells.ToArray());
        }

        return table;
    }

    private void AddPlannerMultiDayNowRow(
        Table table,
        IReadOnlyList<PlannerDayColumnView> columns,
        DateOnly today,
        TimeOnly currentTime,
        DateTime now,
        TuiTheme theme,
        PlannerView? view)
    {
        var meetings = columns.First(column => column.Date == today).CalendarAgenda.Meetings;
        var nextMeeting = viewport.NextMeeting(meetings, currentTime);
        var focus = view?.ActiveFocusBlock;
        var remaining = focus?.Remaining(now);
        var cells = new List<IRenderable>
        {
            new Text(currentTime.ToString("HH:mm"), themeRenderer.Style(theme.Now, Decoration.Bold))
        };
        foreach (var column in columns)
        {
            cells.Add(column.Date != today
                ? new Text("  │", themeRenderer.Style(theme.Muted, Decoration.Dim))
                : new TimelineMarkerRenderable(
                    themeRenderer.Style(theme.Now, Decoration.Bold),
                    nextMeeting is null ? null : nextMeeting.Start - currentTime,
                    nextMeeting?.Title,
                    themeRenderer.Style(theme.Timer, Decoration.Bold),
                    remaining,
                    remaining is null ? null : focus?.TodoTitle,
                    leftPadding: 2));
        }

        table.AddRow(cells.ToArray());
    }

    public IRenderable PlannerDayAllDayCell(
        PlannerView view,
        PlannerDayColumnView column,
        int height,
        TuiTheme theme)
    {
        var paneView = new PlannerView(
            column.IsActive
                ? view.State
                : view.State.RestorePane(column.Date) with { Focus = PlannerFocus.Timeline },
            column.Slots,
            [],
            [])
        {
            CalendarAgenda = column.CalendarAgenda
        };
        IRenderable[] lines;
        if (column.CalendarAgenda.AllDayItems.Length > 0)
        {
            lines = calendarItemRenderer.FitLines(
                column.CalendarAgenda.AllDayItems.Select((item, index) =>
                    GetFitLine(column, theme, paneView, index, item)).ToArray(),
                height, paneView.State.AllDayIndex).ToArray();
        }
        else if (column.IsActive && paneView.State.Focus == PlannerFocus.AllDay)
        {
            lines = [new Text("▶ — ADD ALL-DAY TASK", themeRenderer.Style(theme.AccentBright, Decoration.Bold))];
        }
        else
        {
            lines = [new Text("—", themeRenderer.Style(theme.Muted, Decoration.Dim))];
        }

        var content = new Rows(lines.Take(height).Concat(Enumerable.Repeat<IRenderable>(
                new Text("—", themeRenderer.Style(theme.Muted, Decoration.Dim)),
                Math.Max(0, height - lines.Length)))
            .ToArray());

        return column.IsActive
            ? themeRenderer.OnSurface(content, theme.Surface2, true)
            : content;
    }

    public IRenderable GetFitLine(
        PlannerDayColumnView column,
        TuiTheme theme,
        PlannerView paneView,
        int index,
        PlannerCalendarAllDayItem item)
    {
        var selected = column.IsActive
                       && paneView.State.Focus == PlannerFocus.AllDay
                       && index == paneView.State.AllDayIndex;

        var glyph = item.IsCompleted switch
        {
            true => TodoGlyphs.CompletedTodoGlyph,
            false when item.Assignment is null => "◆",
            false => "○"
        };

        var (color, decoration) = selected switch
        {
            true => (theme.AccentBright, Decoration.Bold),
            false when item.IsCompleted => (theme.Muted, Decoration.Dim),
            false when item.Assignment is null => (theme.Info, Decoration.None),
            false => (theme.Text, Decoration.None)
        };

        var marker = selected ? "▶" : " ";

        return new Text($"{marker} {glyph} {item.Title}",
            themeRenderer.Style(color, decoration)).Ellipsis();
    }
}
