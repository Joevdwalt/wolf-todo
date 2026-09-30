using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerRenderer
{
    private readonly Func<int> widthProvider;
    private readonly Func<int> heightProvider;
    private readonly Func<DateTime> nowProvider;
    private readonly OperationalHeaderRenderer operationalHeaderRenderer = new();
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly StatusRenderer statusRenderer = new();
    private readonly CalendarItemRenderer calendarItemRenderer = new();
    private readonly TodoRowRenderer todoRowRenderer = new();

    public PlannerRenderer()
        : this(TerminalLayout.SafeWindowWidth, TerminalLayout.SafeWindowHeight, null, null)
    {
    }

    public PlannerRenderer(
        Func<int> widthProvider,
        Func<int> heightProvider,
        Func<DateOnly>? todayProvider = null,
        Func<DateTime>? nowProvider = null)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
        this.nowProvider = nowProvider ?? (() => DateTime.Now);
    }

    public void ShowPlanner(
        TabStripView tabs,
        PlannerView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        var context = CreatePlannerRenderContext(view, keyBindings);
        var now = nowProvider();
        RenderPlannerHeader(tabs, view, keyBindings, theme, context, now);

        var timelineTable = view.State.ViewMode == PlannerViewMode.MultiDay && view.DayColumns.Length > 0
            ? CreatePlannerMultiDayTimelineTable(view, context.AvailableRows, theme)
            : CreatePlannerTimelineTable(
                WindowPlannerTimeline(
                    view.Slots,
                    view.State.SlotIndex,
                    context.AvailableRows,
                    view.State.SelectedDate,
                    now,
                    view.CalendarAgenda.Meetings,
                    view.ActiveFocusBlock),
                context.AvailableRows,
                theme);

        RenderPlannerBody(view, theme, context, timelineTable);
        RenderPlannerOverlay(view, keyBindings, theme, context);
        statusRenderer.WritePlannerStatus(context.Status, view, theme, context.EditorDialog);
    }

    public PlannerRenderContext CreatePlannerRenderContext(
        PlannerView view,
        TuiKeyBindings keyBindings)
    {
        var width = widthProvider();
        var height = heightProvider();
        var selectRows = TerminalLayout.SelectListRows(height);
        var textBoxRows = TerminalLayout.TextBoxRows(height);
        var selectList = PlannerSelectList(view, keyBindings);
        var textBox = PlannerTextBox(view);
        var editorDialog = CreatePlannerEditorDialog(view, keyBindings, width, height);
        var status = statusRenderer.PlannerStatus(view, keyBindings, width, height);
        var wideLayout = width >= 120;
        var isMultiDay = view.State.ViewMode == PlannerViewMode.MultiDay && view.DayColumns.Length > 0;
        var allDayVisible = view.CalendarAgenda.AllDayItems.Length > 0 ||
                            view.State.Focus == PlannerFocus.AllDay;
        // Multiday owns all-day content inside each date pane. The legacy
        // full-width panel belongs to the single-day layout only.
        var showAllDayPanel = !isMultiDay && (allDayVisible || (wideLayout && view.State.ShowDetails));
        var wideSidePanels = !isMultiDay && wideLayout && (view.State.ShowDetails || showAllDayPanel);
        var compactDetails = isMultiDay
            ? view.State.Mode == PlannerMode.Browse && view.State.Editor is null && view.CommandPalette is null && view.GlobalCommand is null
            : IsPlannerCompactDetailsVisible(view, wideSidePanels);
        var narrowAllDayHeight = PlannerNarrowAllDayHeight(view, wideSidePanels, showAllDayPanel);
        var pickerHeight = TerminalLayout.PickerHeight(selectList, width, selectRows, textBox, textBoxRows);
        pickerHeight += view.PomodoroPrompt is null ? 0 : PomodoroPromptRenderer.Height;
        var availableRows = PlannerAvailableRows(
            height,
            TerminalLayout.DialogContentHeight(editorDialog) ?? status.Count,
            pickerHeight,
            compactDetails,
            narrowAllDayHeight);
        if (isMultiDay)
        {
            // Multiday keeps all-day content and a compact selected-item panel
            // below the shared timeline. Keep those rows inside the viewport.
            var allDayRows = Math.Max(1, Math.Min(4, view.DayColumns.IsDefaultOrEmpty
                ? 1
                : view.DayColumns.Max(column => column.CalendarAgenda.AllDayItems.Length)));
            availableRows = Math.Max(1, availableRows - allDayRows);
        }
        var timelineWidth = wideSidePanels ? Math.Max(40, (width * 2 / 3) - 2) : width;

        return new PlannerRenderContext(
            width,
            height,
            selectList,
            selectRows,
            textBox,
            textBoxRows,
            view.PomodoroPrompt,
            editorDialog,
            status,
            wideSidePanels,
            showAllDayPanel,
            compactDetails,
            narrowAllDayHeight,
            availableRows,
            timelineWidth);
    }

    public void RenderPlannerHeader(
        TabStripView tabs,
        PlannerView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme,
        PlannerRenderContext context,
        DateTime now)
    {
        var rangeStart = view.State.ViewMode == PlannerViewMode.MultiDay
            ? view.State.VisibleStartDate ?? view.State.SelectedDate
            : (DateOnly?)null;
        var rangeEnd = rangeStart?.AddDays(view.State.VisibleDayCount - 1);
        operationalHeaderRenderer.Write(
            tabs,
            keyBindings,
            theme,
            context.Width,
            statusRenderer.PlannerMode(view),
            view.State.SelectedDate,
            now,
            view.OpenTodoCount,
            view.ProjectErrorCount,
            rangeStart,
            rangeEnd);
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
        var todayColumn = columns.FirstOrDefault(column => column.Date == today);
        var showNow = todayColumn is not null && availableRows >= 2;
        var markerSlotIndex = todayColumn is null
            ? -1
            : Enumerable.Range(0, todayColumn.Slots.Length)
                .FirstOrDefault(index => todayColumn.Slots[index].Time >= currentTime, -1);
        if (todayColumn is not null && markerSlotIndex < 0)
        {
            markerSlotIndex = todayColumn.Slots.Length;
        }

        var slotsToRender = WindowPlannerMultiDaySlots(
            columns,
            selectedSlotIndex,
            Math.Max(1, availableRows - (showNow ? 1 : 0)));
        if (showNow && slotsToRender.Count > 0)
        {
            var todaySlots = todayColumn!.Slots;
            var markerAnchor = Math.Clamp(markerSlotIndex, 0, todaySlots.Length - 1);
            var requiredStart = Math.Min(selectedSlotIndex, markerAnchor);
            var requiredEnd = Math.Max(selectedSlotIndex, markerAnchor);
            var slotBudget = Math.Max(1, availableRows - 1);
            if (requiredEnd - requiredStart + 1 <= slotBudget)
            {
                var start = requiredStart;
                var end = requiredEnd + 1;
                while (start > 0 && end - start < slotBudget)
                {
                    start--;
                }

                while (end < todaySlots.Length && end - start < slotBudget)
                {
                    end++;
                }

                slotsToRender = Enumerable.Range(start, end - start).ToArray();
            }
        }

        var includesNow = showNow && slotsToRender.Count > 0 &&
                          markerSlotIndex >= slotsToRender[0] &&
                          markerSlotIndex <= slotsToRender[^1] + 1;
        foreach (var slotIndex in slotsToRender)
        {
            if (includesNow && markerSlotIndex == slotIndex)
            {
                AddPlannerMultiDayNowRow(table, columns, today, currentTime, now, theme, view);
            }

            var slotRows = columns
                .Select(column => PlannerTimelineRenderModel.ForSlot(column.Slots[slotIndex]))
                .ToArray();
            var activeRows = slotRows.FirstOrDefault(rows => rows.Any(renderRow => renderRow.IsSelected));
            var timeCell = PlannerTimeRulerLine(
                activeRows?[0] ?? slotRows[0][0],
                theme,
                selected: activeRows is not null);
            var cells = new List<IRenderable> { timeCell };
            cells.AddRange(columns.Select((column, index) => PlannerMultiDayTimelineCell(
                column.Slots[slotIndex], slotRows[index], column.Date, paneWidth, theme, view?.ActiveFocusBlock)));
            table.AddRow(cells.ToArray());
        }

        if (includesNow && markerSlotIndex > slotsToRender[^1])
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
        var nextMeeting = NextMeeting(meetings, currentTime);
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

    private IRenderable PlannerMultiDayTimelineCell(
        PlannerSlotView slot,
        IReadOnlyList<PlannerTimelineRenderRow> rows,
        DateOnly date,
        int contentWidth,
        TuiTheme theme,
        PlannerFocusBlock? activeFocusBlock)
    {
        const int separatorWidth = 2;
        if (slot.Items.Length == 0)
        {
            var empty = rows[0];
            return empty.IsSelected
                ? PlannerTimelineRenderLine(CompactPlannerMultiDayRow(empty, null, contentWidth, false, true), theme)
                : new Text("  │", themeRenderer.Style(theme.Muted, Decoration.Dim));
        }

        // Earlier starts retain their path when another item joins the slot.
        // Equal starts retain the presenter's stable item order.
        var itemRows = slot.Items.Select((item, index) => (Item: item, Row: rows[index], Index: index))
            .OrderBy(entry => entry.Item.Start)
            .ThenBy(entry => entry.Index)
            .ToArray();
        var capacity = Math.Max(1, (contentWidth + separatorWidth) / (8 + separatorWidth));
        var visibleItemCount = itemRows.Length <= capacity ? itemRows.Length : Math.Max(1, capacity - 1);
        var selectedIndex = Array.FindIndex(itemRows, entry => entry.Row.IsSelected || entry.Row.IsActive);
        var start = itemRows.Length <= visibleItemCount || selectedIndex < 0
            ? 0
            : Math.Clamp(selectedIndex - visibleItemCount / 2, 0, itemRows.Length - visibleItemCount);
        var visibleRows = itemRows.Skip(start).Take(visibleItemCount).ToArray();
        var hasOverflow = itemRows.Length > visibleItemCount;
        var entryCount = visibleRows.Length + (hasOverflow ? 1 : 0);
        var separatorCount = entryCount - 1;
        var availableSegmentWidth = Math.Max(entryCount, contentWidth - separatorCount * separatorWidth);
        var naturalWidths = visibleRows.Select((entry, index) => PlannerMultiDayNaturalWidth(
                entry.Row, entry.Item, ShowMultiDayFinish(entry.Item, date, activeFocusBlock), index == 0))
            .Concat(hasOverflow ? [$" +{itemRows.Length - visibleItemCount}".Length] : [])
            .ToArray();
        var fitsNaturally = naturalWidths.Sum() <= availableSegmentWidth;
        var segmentWidths = fitsNaturally
            ? naturalWidths
            : Enumerable.Range(0, entryCount)
                .Select(index => availableSegmentWidth / entryCount +
                                 (index < availableSegmentWidth % entryCount ? 1 : 0))
                .ToArray();
        var entries = visibleRows.Select((entry, index) =>
        {
            var compact = CompactPlannerMultiDayRow(entry.Row, entry.Item, segmentWidths[index],
                ShowMultiDayFinish(entry.Item, date, activeFocusBlock), index == 0);
            var line = compact.StatusGlyph.Length == 0
                ? PlannerMultiDayPathLine(compact, theme)
                : PlannerTimelineRenderLine(compact, theme);
            return entry.Row.IsActive || entry.Row.IsSelected
                ? themeRenderer.OnSurface(line, theme.Surface2)
                : line;
        }).ToList();
        if (hasOverflow)
        {
            entries.Add(new Text($" +{itemRows.Length - visibleItemCount}",
                themeRenderer.Style(theme.Warning, Decoration.Bold)));
        }

        var cell = new Table().NoBorder().Collapse().HideHeaders();
        var cellContents = new List<IRenderable>();
        for (var index = 0; index < entries.Count; index++)
        {
            cell.AddColumn(new TableColumn(entries[index])
            {
                Width = segmentWidths[index],
                NoWrap = true,
                Padding = new Padding(0, 0)
            });
            cellContents.Add(entries[index]);
            if (index < separatorCount)
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
        var color = row.IsActive || row.IsSelected ? theme.AccentBright :
            row.ItemType == PlannerItemType.Pomodoro ? theme.Timer : theme.BorderActive;
        return new Text(row.BranchGlyph, themeRenderer.Style(color,
            row.IsActive || row.IsSelected ? Decoration.Bold : Decoration.None)).Ellipsis();
    }

    private static bool ShowMultiDayFinish(
        PlannerTimelineItemView item,
        DateOnly date,
        PlannerFocusBlock? activeFocusBlock) =>
        item.TimeShape == PlannerTimeShape.Duration &&
        item.IntervalState is (PlannerIntervalState.End or PlannerIntervalState.StartAndEnd) &&
        (item.ItemType != PlannerItemType.Pomodoro ||
         activeFocusBlock is null ||
         activeFocusBlock.EndsAt <= date.ToDateTime(new TimeOnly(22, 0)));

    private static int PlannerMultiDayNaturalWidth(
        PlannerTimelineRenderRow row,
        PlannerTimelineItemView item,
        bool showFinish,
        bool firstSegment)
    {
        var compact = CompactPlannerMultiDayRow(row, item, int.MaxValue, showFinish, firstSegment);
        return compact.BranchGlyph.GetCellWidth() +
               (compact.StatusGlyph.Length == 0 ? 0 :
                   1 + compact.StatusGlyph.GetCellWidth() + 1 + compact.Title.GetCellWidth() +
                   (compact.Metadata.Length == 0 ? 0 : 1 + compact.Metadata.GetCellWidth()));
    }

    private static PlannerTimelineRenderRow CompactPlannerMultiDayRow(
        PlannerTimelineRenderRow row,
        PlannerTimelineItemView? item,
        int width,
        bool showFinish,
        bool firstSegment)
    {
        if (row.IsEmpty)
        {
            return row with { BranchGlyph = row.IsSelected ? "▶ │" : "  │", IsSelectionBridge = false };
        }

        if (row.StatusGlyph.Length == 0)
        {
            var path = showFinish ? $"→│{item!.End:HH:mm}" : "│";
            var branch = showFinish
                ? (row.IsSelected ? (firstSegment ? "▶" : " ▶") : " ") + path
                : row.IsSelected ? (firstSegment ? "▶ │" : " ▶ │") : "  │";
            return row with { BranchGlyph = branch, Title = string.Empty,
                Metadata = string.Empty, IsSelectionBridge = false };
        }

        var metadata = showFinish ? $"→│{item!.End:HH:mm}" : string.Empty;
        var branchGlyph = row.IsSelected ? (firstSegment ? "▶" : " ▶") : " ";
        var fixedWidth = branchGlyph.GetCellWidth() + 1 + row.StatusGlyph.GetCellWidth() + 1;
        var available = Math.Max(0, width - fixedWidth);
        if (metadata.Length + 1 > available)
        {
            metadata = string.Empty;
        }

        var titleWidth = Math.Max(0, available - (metadata.Length == 0 ? 0 : metadata.Length + 1));
        var title = row.Title.Length <= titleWidth
            ? row.Title
            : titleWidth switch
            {
                0 => string.Empty,
                1 => "…",
                _ => row.Title[..(titleWidth - 1)].TrimEnd() + "…"
            };
        return row with { BranchGlyph = branchGlyph, Title = title,
            Metadata = metadata, IsSelectionBridge = false };
    }

    public IReadOnlyList<int> WindowPlannerMultiDaySlots(
        IReadOnlyList<PlannerDayColumnView> columns,
        int selectedSlotIndex,
        int availableRows)
    {
        var slotCount = columns.Min(column => column.Slots.Length);
        // Multiday overlaps share one horizontal row, so a busy date never
        // consumes additional vertical space in the synchronized window.
        var heights = Enumerable.Repeat(1, slotCount).ToArray();
        if (heights.Sum() <= availableRows)
        {
            return Enumerable.Range(0, slotCount).ToArray();
        }

        var selected = Math.Clamp(selectedSlotIndex, 0, slotCount - 1);
        var start = selected;
        var usedRows = heights[selected];
        while (start > 0 && usedRows + heights[start - 1] <= availableRows)
        {
            start--;
            usedRows += heights[start];
        }

        var end = selected + 1;
        while (end < slotCount && usedRows + heights[end] <= availableRows)
        {
            usedRows += heights[end];
            end++;
        }

        return Enumerable.Range(start, end - start).ToArray();
    }

    public IRenderable PlannerTimelineCell(
        IReadOnlyList<PlannerTimelineRenderRow> renderRows,
        TuiTheme theme) =>
        PlannerTimelineCell(renderRows, renderRows.Count, theme);

    public IRenderable PlannerTimelineCell(
        IReadOnlyList<PlannerTimelineRenderRow> renderRows,
        int height,
        TuiTheme theme) =>
        new Rows(renderRows.Select(row =>
        {
            var content = PlannerTimelineRenderLine(row, theme);
            var activeItem = !row.IsEmpty && (row.IsActive || row.IsSelected);
            var selectedEmptySlot = row.IsEmpty && row.IsSelected;
            return activeItem
                ? themeRenderer.OnSurface(content, theme.Surface2)
                : selectedEmptySlot
                    ? themeRenderer.OnSurface(content, theme.Surface2, true)
                    : content;
        }).Concat(Enumerable.Range(renderRows.Count, height - renderRows.Count)
            .Select(_ => PlannerTimelinePaddingLine(theme)))
            .ToArray());

    private IRenderable PlannerTimelinePaddingLine(TuiTheme theme) =>
        new Text("│", themeRenderer.Style(theme.Muted, Decoration.Dim));

    private IRenderable PlannerDayAllDayCell(
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
        var lines = column.CalendarAgenda.AllDayItems.Length == 0
            ? column.IsActive && paneView.State.Focus == PlannerFocus.AllDay
                ? new IRenderable[] { new Text("▶ — ADD ALL-DAY TASK", themeRenderer.Style(theme.AccentBright, Decoration.Bold)) }
                : [new Text("—", themeRenderer.Style(theme.Muted, Decoration.Dim))]
            : calendarItemRenderer.FitLines(column.CalendarAgenda.AllDayItems.Select((item, index) =>
            {
                var selected = column.IsActive && paneView.State.Focus == PlannerFocus.AllDay && index == paneView.State.AllDayIndex;
                var glyph = item.IsCompleted ? "✓" : item.Assignment is null ? "◆" : "○";
                var color = selected ? theme.AccentBright : item.IsCompleted ? theme.Muted : item.Assignment is null ? theme.Info : theme.Text;
                return (IRenderable)new Text($"{(selected ? "▶" : " ")} {glyph} {item.Title}",
                    themeRenderer.Style(color, selected ? Decoration.Bold : item.IsCompleted ? Decoration.Dim : Decoration.None)).Ellipsis();
            }).ToArray(), height, paneView.State.AllDayIndex).ToArray();
        var content = new Rows(lines.Take(height).Concat(Enumerable.Range(lines.Length, Math.Max(0, height - lines.Length))
            .Select(_ => (IRenderable)new Text("—", themeRenderer.Style(theme.Muted, Decoration.Dim)))
            .ToArray()));
        return column.IsActive
            ? themeRenderer.OnSurface(content, theme.Surface2, true)
            : content;
    }

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
                var time = PlannerTimeRulerLine(renderRow, theme);
                var content = PlannerTimelineRenderLine(renderRow, theme);
                var isActiveItem = !renderRow.IsEmpty && (renderRow.IsActive || renderRow.IsSelected);
                var isSelectedEmptySlot = renderRow.IsEmpty && slot.IsSelected;
                table.AddRow(
                    isSelectedEmptySlot ? themeRenderer.OnSurface(time, theme.Surface2, true) : time,
                    isActiveItem
                        ? themeRenderer.OnSurface(content, theme.Surface2)
                        : isSelectedEmptySlot
                            ? themeRenderer.OnSurface(content, theme.Surface2, true)
                            : content);
            }
        }
    }

    public void PadPlannerTimeline(
        Table table,
        IReadOnlyList<PlannerTimelineRow> timelineRows,
        int availableRows)
    {
        for (var index = PlannerTimelineHeight(timelineRows); index < availableRows; index++)
        {
            table.AddEmptyRow();
        }
    }

    public void RenderPlannerBody(
        PlannerView view,
        TuiTheme theme,
        PlannerRenderContext context,
        Table timelineTable)
    {
        if (context.EditorDialog is not null && context.AvailableRows <= 1)
        {
            return;
        }

        if (context.WideSidePanels)
        {
            RenderPlannerWideBody(view, theme, context, timelineTable);
            return;
        }

        RenderPlannerNarrowBody(view, theme, context, timelineTable);
    }

    public void RenderPlannerWideBody(
        PlannerView view,
        TuiTheme theme,
        PlannerRenderContext context,
        Table timelineTable)
    {
        var detailWidth = Math.Max(28, context.Width - context.TimelineWidth - 4);
        const int inspectorContentHeight = 11;
        var allDayContentHeight = Math.Max(
            1,
            context.AvailableRows - (view.State.ShowDetails ? inspectorContentHeight + 2 : 0));
        var sidePanels = new List<IRenderable>();
        if (view.State.ShowDetails)
        {
            sidePanels.Add(PlannerPanel(
                "INSPECTOR",
                new HeightConstrainedRenderable(
                    CreateContent(PlannerDetailLines(view, theme)),
                    inspectorContentHeight),
                theme));
        }

        if (context.ShowAllDayPanel)
        {
            sidePanels.Add(PlannerPanel(
                "ALL DAY",
                CreateContent(calendarItemRenderer.AllDayAgendaLines(view, theme, allDayContentHeight)),
                theme,
                view.State.Focus == PlannerFocus.AllDay));
        }

        var shell = new Table().NoBorder().Collapse().HideHeaders();
        shell.AddColumn(new TableColumn(string.Empty).Width(context.TimelineWidth).NoWrap());
        shell.AddColumn(new TableColumn(string.Empty).Width(detailWidth).NoWrap());
        shell.AddRow(
            timelineTable,
            themeRenderer.OnSurface(
                new Rows(sidePanels),
                theme.Surface2,
                true));
        themeRenderer.WriteSurface(shell, theme.Surface, true);
    }

    public void RenderPlannerNarrowBody(
        PlannerView view,
        TuiTheme theme,
        PlannerRenderContext context,
        Table timelineTable)
    {
        themeRenderer.WriteSurface(timelineTable, theme.Surface, true);
        if (context.CompactDetails)
        {
            themeRenderer.WriteSurface(
                new Panel(PlannerCompactDetail(view, theme))
                {
                    Header = new PanelHeader("SELECTED"),
                    Border = BoxBorder.Square,
                    BorderStyle = themeRenderer.Style(theme.Border),
                    Expand = true
                },
                theme.Surface2,
                true);
        }

        if (context.NarrowAllDayHeight > 0)
        {
            themeRenderer.WriteSurface(
                PlannerPanel(
                    "ALL DAY",
                    CreateContent(calendarItemRenderer.AllDayAgendaLines(
                        view,
                        theme,
                        Math.Max(1, context.NarrowAllDayHeight - 2))),
                    theme,
                    view.State.Focus == PlannerFocus.AllDay),
                theme.Surface2,
                true);
        }
    }

    public void RenderPlannerOverlay(
        PlannerView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme,
        PlannerRenderContext context)
    {
        if (context.PomodoroPrompt is { } pomodoroPrompt)
        {
            AnsiConsole.Write(PomodoroPromptRenderer.Render(pomodoroPrompt, theme, context.Width));
        }
        else if (context.SelectList is not null)
        {
            AnsiConsole.Write(SelectList.Default.Render(
                context.SelectList,
                theme,
                new TuiComponentConstraints(context.Width, context.SelectRows)));
        }
        else if (context.TextBox is { } activeTextBox)
        {
            AnsiConsole.Write(MultilineTextBox.Default.Render(
                activeTextBox,
                theme,
                new TuiComponentConstraints(context.Width, context.TextBoxRows),
                TuiKeyBindings.ShortestDisplayName(keyBindings.SaveForm)));
        }
    }

    public TodoTaskEditorDialogView? CreatePlannerEditorDialog(
        PlannerView view,
        TuiKeyBindings keyBindings,
        int width,
        int height) =>
        view.State.Editor is { } editor
            ? TodoTaskEditorDialog.Create(editor, keyBindings, width, height)
            : null;

    public bool IsPlannerCompactDetailsVisible(PlannerView view, bool wideSidePanels) =>
        view.State.ShowDetails && !wideSidePanels &&
        view.State.Mode == PlannerMode.Browse &&
        view.State.Editor is null &&
        view.CommandPalette is null &&
        view.GlobalCommand is null;

    public int PlannerNarrowAllDayHeight(
        PlannerView view,
        bool wideSidePanels,
        bool showAllDayPanel) =>
        !wideSidePanels && showAllDayPanel
            ? Math.Min(6, view.CalendarAgenda.AllDayItems.Length + 3)
            : 0;

    public int PlannerAvailableRows(
        int terminalHeight,
        int statusHeight,
        int pickerHeight,
        bool compactDetails,
        int narrowAllDayHeight)
    {
        const int tabTableStatusBorderAndCursorHeight = 8;
        const int compactDetailsHeight = 3;
        var reservedHeight = tabTableStatusBorderAndCursorHeight + pickerHeight +
                             (compactDetails ? compactDetailsHeight : 0) + narrowAllDayHeight;
        return Math.Max(1, terminalHeight - statusHeight - reservedHeight);
    }

    public IReadOnlyList<PlannerTimelineRow> WindowPlannerTimeline(
        IReadOnlyList<PlannerSlotView> slots,
        int selectedIndex,
        int availableRows,
        DateOnly selectedDate,
        DateTime now,
        IReadOnlyList<PlannerCalendarMeeting>? meetings = null,
        PlannerFocusBlock? activeFocusBlock = null)
    {
        var rows = new List<PlannerTimelineRow>(slots.Count + 1);
        var today = DateOnly.FromDateTime(now);
        var currentTime = new TimeOnly(now.Hour, now.Minute);
        var addMarker = selectedDate == today;
        var nextMeeting = addMarker ? NextMeeting(meetings ?? [], currentTime) : null;
        TimeSpan? timeUntilNextMeeting = nextMeeting is null ? null : nextMeeting.Start - currentTime;
        TimeSpan? pomodoroRemaining = addMarker && activeFocusBlock is not null
            ? activeFocusBlock.Remaining(now)
            : null;
        var pomodoroTitle = pomodoroRemaining is not null ? activeFocusBlock?.TodoTitle : null;
        var markerAdded = false;
        foreach (var slot in slots)
        {
            if (addMarker && !markerAdded && currentTime <= slot.Time)
            {
                rows.Add(new PlannerNowTimelineRow(
                    currentTime,
                    timeUntilNextMeeting,
                    nextMeeting?.Title,
                    pomodoroRemaining,
                    pomodoroTitle));
                markerAdded = true;
            }

            rows.Add(new PlannerSlotTimelineRow(slot));
        }

        if (addMarker && !markerAdded)
        {
            rows.Add(new PlannerNowTimelineRow(
                currentTime,
                timeUntilNextMeeting,
                nextMeeting?.Title,
                pomodoroRemaining,
                pomodoroTitle));
        }

        if (PlannerTimelineHeight(rows) <= availableRows)
        {
            return rows;
        }

        var selectedRow = rows.FindIndex(row =>
            row is PlannerSlotTimelineRow slotRow && slotRow.Slot.IsSelected);
        if (selectedRow < 0)
        {
            selectedRow = Math.Clamp(selectedIndex, 0, rows.Count - 1);
        }

        var start = selectedRow;
        var usedRows = TimelineRowHeight(rows[selectedRow]);
        while (start > 0 && usedRows + TimelineRowHeight(rows[start - 1]) <= availableRows)
        {
            start--;
            usedRows += TimelineRowHeight(rows[start]);
        }

        var end = selectedRow + 1;
        while (end < rows.Count && usedRows + TimelineRowHeight(rows[end]) <= availableRows)
        {
            usedRows += TimelineRowHeight(rows[end]);
            end++;
        }

        var markerRow = rows.FindIndex(row => row is PlannerNowTimelineRow);
        if (markerRow >= 0)
        {
            var requiredStart = Math.Min(selectedRow, markerRow);
            var requiredEnd = Math.Max(selectedRow, markerRow) + 1;
            var requiredHeight = PlannerTimelineHeight(rows.Skip(requiredStart).Take(requiredEnd - requiredStart));
            if (requiredHeight <= availableRows)
            {
                start = requiredStart;
                end = requiredEnd;
                usedRows = requiredHeight;
                while (start > 0 && usedRows + TimelineRowHeight(rows[start - 1]) <= availableRows)
                {
                    start--;
                    usedRows += TimelineRowHeight(rows[start]);
                }

                while (end < rows.Count && usedRows + TimelineRowHeight(rows[end]) <= availableRows)
                {
                    usedRows += TimelineRowHeight(rows[end]);
                    end++;
                }
            }
        }

        return rows.Skip(start).Take(end - start).ToArray();
    }

    private static PlannerCalendarMeeting? NextMeeting(
        IReadOnlyList<PlannerCalendarMeeting> meetings,
        TimeOnly currentTime)
    {
        var nextMeeting = meetings
            .Where(meeting => meeting.Start > currentTime)
            .OrderBy(meeting => meeting.Start)
            .ThenBy(meeting => meeting.End)
            .ThenBy(meeting => meeting.Title, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return nextMeeting;
    }

    public int PlannerTimelineHeight(IEnumerable<PlannerTimelineRow> rows) =>
        rows.Sum(TimelineRowHeight);

    public int TimelineRowHeight(PlannerTimelineRow row) => row switch
    {
        PlannerSlotTimelineRow slot => PlannerTimelineRenderModel.ForSlot(slot.Slot).Count,
        _ => 1
    };

    public SelectListView? PlannerSelectList(PlannerView view, TuiKeyBindings bindings)
    {
        if (view.CommandPalette is not null)
        {
            return CommandPaletteSelectList(view.CommandPalette, bindings);
        }

        if (view.State.Editor is not null)
        {
            return TodoEditorSelectList(
                view.State.Editor,
                view.Projects.Select(project => new TodoEditorProjectOption(project.Title, project.Path)).ToArray(),
                bindings);
        }

        if (view.State.Mode is not (PlannerMode.ChooseTodo or PlannerMode.EditFilter))
        {
            return null;
        }

        var searchText = view.State.Mode == PlannerMode.EditFilter
            ? view.State.FilterDraft
            : view.State.FilterText.Length == 0 ? null : view.State.FilterText;
        return new SelectListView(
            "Unscheduled todos",
            view.PickerTodos
                .Select(todo => new SelectOption(todo.Todo.Title, $"[{todo.ProjectTitle}]"))
                .ToArray(),
            view.State.PickerIndex,
            searchText,
            "No open unscheduled todos",
            statusRenderer.PlannerPickerFooter(bindings),
            view.State.Error);
    }

    public SelectListView CommandPaletteSelectList(CommandPaletteView palette, TuiKeyBindings bindings) =>
        new(
            "Command palette",
            palette.Items.Select(item => new SelectOption(
                $"{item.Group}: {item.Label}",
                $"[{item.Binding}]" + (item.IsEnabled ? string.Empty : $" — {item.DisabledReason}"),
                item.IsEnabled)).ToArray(),
            palette.SelectedIndex,
            palette.State.IsSearching ? palette.State.Query : null,
            "No matching actions",
            statusRenderer.CommandPaletteFooter(bindings),
            palette.State.Error);

    public SelectListView? TodoEditorSelectList(
        TodoTaskEditorState editor,
        IReadOnlyList<TodoEditorProjectOption> projects,
        TuiKeyBindings bindings)
    {
        if (editor.IsEditingContent)
        {
            return null;
        }

        if (editor.IsChoosingProject)
        {
            return new SelectListView(
                "Choose project",
                projects.Select(project => new SelectOption(project.Title)).ToArray(),
                editor.ProjectPickerIndex,
                null,
                "No valid projects",
                $"{statusRenderer.Shortest(bindings.MoveDown)}/{statusRenderer.Shortest(bindings.MoveUp)} MOVE  " +
                $"{statusRenderer.Shortest(bindings.Open)} SELECT  {statusRenderer.Shortest(bindings.Back)} CANCEL",
                editor.Error);
        }

        return null;
    }

    public MultilineTextBoxState? PlannerTextBox(PlannerView view) =>
        view.State.Editor is null ? null : TodoEditorTextBox(view.State.Editor);

    public MultilineTextBoxState? TodoEditorTextBox(TodoTaskEditorState editor) =>
        editor.ContentTextBox;

    public IReadOnlyList<IRenderable> PlannerDetailLines(PlannerView view, TuiTheme theme)
    {
        if (view.State.Mode == PlannerMode.MoveTodo && view.State.MovingTodo is { } movingIdentity)
        {
            var moving = view.Slots
                .SelectMany(slot => slot.Assignments)
                .Concat(view.CalendarAgenda.AllDayItems
                    .Where(item => item.Assignment is not null)
                    .Select(item => item.Assignment!))
                .FirstOrDefault(assignment => assignment.Identity == movingIdentity);
            if (moving is not null)
            {
                var schedule = moving.Todo.Schedule;
                var duration = moving.Todo.Duration;
                var destination = view.State.Focus == PlannerFocus.AllDay
                    ? $"{view.State.SelectedDate:yyyy-MM-dd} · ALL DAY"
                    : duration is { } destinationDuration
                        ? $"{view.State.SelectedDate:yyyy-MM-dd} " +
                          $"{view.SelectedSlot.Time:HH:mm}–{view.SelectedSlot.Time.Add(destinationDuration):HH:mm}"
                        : $"{view.State.SelectedDate:yyyy-MM-dd} {view.SelectedSlot.Time:HH:mm}";
                var current = schedule is null
                    ? "Unscheduled"
                    : schedule.Time is null
                        ? $"{schedule.Date:yyyy-MM-dd} · ALL DAY"
                        : duration is { } currentDuration
                            ? $"{schedule.Date:yyyy-MM-dd} " +
                              $"{schedule.Time:HH:mm}–{schedule.Time.Value.Add(currentDuration):HH:mm}"
                            : $"{schedule.Date:yyyy-MM-dd} {schedule.Time:HH:mm}";
                return
                [
                    new Text("MOVE TASK", themeRenderer.Style(theme.AccentBright, Decoration.Bold)),
                    new Text($"Task: {moving.Todo.Title}", themeRenderer.Style(theme.Text)),
                    new Text($"LINK: {TaskLinkCode.Generate(moving.Identity.ProjectPath, moving.Identity.SourceLine)}", themeRenderer.Style(theme.Info)),
                    new Text($"Current: {current}", themeRenderer.Style(theme.Date)),
                    new Text($"Destination: {destination}", themeRenderer.Style(theme.Date)),
                    new Text($"Duration: {todoRowRenderer.FormatDuration(duration) ?? "Instant"}", themeRenderer.Style(theme.Info))
                ];
            }
        }

        if (view.State.Focus == PlannerFocus.AllDay)
        {
            return calendarItemRenderer.AllDayDetailLines(view, theme);
        }

        if (view.SelectedAssignment is null)
        {
            return view.SelectedMeeting is null
                ? [new Text("EMPTY TIMESLOT", themeRenderer.Style(theme.Muted, Decoration.Dim))]
                : calendarItemRenderer.MeetingDetailLines(view, theme);
        }

        var assignment = view.SelectedAssignment;
        var todo = assignment.Todo;
        var lines = new List<IRenderable>
        {
            new Text(todo.Title, themeRenderer.Style(theme.Heading, Decoration.Bold))
        };
        if (view.SelectedSlot.Assignments.Length > 1)
        {
            lines.Add(new Text(
                $"{view.SelectedSlot.Assignments.Length} STACKED TASKS · J/K SELECT",
                themeRenderer.Style(theme.Info, Decoration.Bold)));
        }

        calendarItemRenderer.AddField(lines, "Project", assignment.ProjectTitle, theme, theme.Text);
        calendarItemRenderer.AddField(lines, "Link", TaskLinkCode.Generate(assignment.Identity.ProjectPath, assignment.Identity.SourceLine), theme, theme.Info);
        if (!string.IsNullOrEmpty(todo.SectionPath))
        {
            calendarItemRenderer.AddField(lines, "Section", todo.SectionPath, theme, theme.Text);
        }

        calendarItemRenderer.AddField(lines, "Reference", todo.ExternalReference, theme, theme.Info);
        calendarItemRenderer.AddField(lines, "Priority", todo.Priority?.ToString(), theme, todoRowRenderer.PriorityColor(todo.Priority, theme));
        calendarItemRenderer.AddField(
            lines,
            "Tags",
            todo.Tags.Length == 0 ? null : string.Join(", ", todo.Tags.Select(tag => $"#{tag}")),
            theme,
            theme.Tag);
        calendarItemRenderer.AddField(
            lines,
            "Scheduled",
            todo.Schedule is null ? null : todoRowRenderer.FormatSchedule(todo.Schedule),
            theme,
            theme.Date);
        calendarItemRenderer.AddField(lines, "Duration", todoRowRenderer.FormatDuration(todo.Duration), theme, theme.Info);
        calendarItemRenderer.AddField(
            lines,
            "Calendar",
            view.SelectedSlot.Meetings.FirstOrDefault() is null
                ? null
                : calendarItemRenderer.MeetingLabel(view.SelectedSlot.Meetings[0]) +
                  (view.SelectedSlot.Meetings.Length > 1 ? $" +{view.SelectedSlot.Meetings.Length - 1}" : string.Empty),
            theme,
            theme.Info);

        if (todo.Notes.Length > 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("NOTES", themeRenderer.Style(theme.Heading, Decoration.Bold)));
            lines.AddRange(todo.Notes.Select(note => new Text($"• {note.Text}", themeRenderer.Style(theme.Text))));
        }

        if (todo.Subtasks.Length > 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("SUBTASKS", themeRenderer.Style(theme.Heading, Decoration.Bold)));
            lines.AddRange(todo.Subtasks.Select(subtask => todoRowRenderer.DetailLine(subtask, [], false, theme)));
        }

        if (todo.Notes.Length == 0 && todo.Subtasks.Length == 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("NO ADDITIONAL DETAILS", themeRenderer.Style(theme.Muted, Decoration.Dim)));
        }

        return lines;
    }

    public Panel PlannerPanel(
        string header,
        IRenderable content,
        TuiTheme theme,
        bool active = false)
    {
        var styledHeader = new System.Text.StringBuilder();
        themeRenderer.AppendStyled(styledHeader, header, theme.AccentBright, Decoration.Bold);
        return new Panel(content)
        {
            Header = new PanelHeader(styledHeader.ToString()),
            Border = BoxBorder.Square,
            BorderStyle = themeRenderer.Style(active ? theme.AccentBright : theme.BorderActive),
            Expand = true
        };
    }

    public IRenderable PlannerCompactDetail(PlannerView view, TuiTheme theme)
    {
        if (view.State.ViewMode == PlannerViewMode.MultiDay)
        {
            return PlannerMultiDayCompactDetail(view, theme);
        }

        if (view.State.Focus == PlannerFocus.AllDay)
        {
            var item = view.SelectedAllDayItem;
            if (item is null)
            {
                return new Text("Empty all-day schedule", themeRenderer.Style(theme.Muted, Decoration.Dim));
            }

            var label = item.Assignment is null
                ? $"{item.Title}  ·  {calendarItemRenderer.AllDayKindLabel(item.Kind)}  ·  READ ONLY"
                : $"LINK: {TaskLinkCode.Generate(item.Assignment.Identity.ProjectPath, item.Assignment.Identity.SourceLine)}  ·  {item.Title}  ·  {item.ProjectTitle}  ·  ALL DAY";
            return new Text(
                label,
                themeRenderer.Style(item.Assignment is null ? theme.Info : theme.Heading, Decoration.Bold)).Ellipsis();
        }

        if (view.SelectedAssignment is null)
        {
            if (view.SelectedMeeting is null)
            {
                return new Text("Empty timeslot", themeRenderer.Style(theme.Muted, Decoration.Dim));
            }

            var meeting = view.SelectedMeeting;
            var meetingLine = new System.Text.StringBuilder();
            themeRenderer.AppendStyled(meetingLine, meeting.Title, theme.Info, Decoration.Bold);
            themeRenderer.AppendStyled(meetingLine, $"  {calendarItemRenderer.MeetingTimeAndDuration(meeting)}", theme.Muted, Decoration.Dim);
            return new Markup(meetingLine.ToString()).Ellipsis();
        }

        var assignment = view.SelectedAssignment;
        var todo = assignment.Todo;
        var metadata = new[]
        {
            assignment.ProjectTitle,
            todo.Priority?.ToString(),
            todo.Tags.Length == 0 ? null : string.Join(' ', todo.Tags.Select(tag => $"#{tag}")),
            todo.Schedule is null ? null : todoRowRenderer.FormatSchedule(todo.Schedule)
        };
        var line = new System.Text.StringBuilder();
        themeRenderer.AppendStyled(line,
            $"LINK: {TaskLinkCode.Generate(assignment.Identity.ProjectPath, assignment.Identity.SourceLine)}  ·  ", theme.Info);
        themeRenderer.AppendStyled(line, todo.Title, theme.Heading, Decoration.Bold);
        if (view.SelectedSlot.Assignments.Length > 1)
        {
            themeRenderer.AppendStyled(
                line,
                $"  {view.SelectedSlot.Assignments.Length} STACKED · J/K SELECT",
                theme.Info,
                Decoration.Bold);
        }

        themeRenderer.AppendStyled(
            line,
            $"  {string.Join(" · ", metadata.Where(value => !string.IsNullOrEmpty(value)))}",
            theme.Muted,
            Decoration.Dim);
        return new Markup(line.ToString()).Ellipsis();
    }

    private IRenderable PlannerMultiDayCompactDetail(PlannerView view, TuiTheme theme)
    {
        var date = view.State.SelectedDate;
        var dateLabel = date.ToString("ddd dd").ToUpperInvariant();
        if (view.State.Focus == PlannerFocus.AllDay)
        {
            var item = view.SelectedAllDayItem;
            if (item is null)
            {
                return new Text($"{dateLabel} · ALL DAY · Empty destination",
                    themeRenderer.Style(theme.Muted, Decoration.Dim)).Ellipsis();
            }

            var estimate = item.Assignment?.Todo.Duration is { } duration
                ? $" · {todoRowRenderer.FormatDuration(duration)} estimate"
                : string.Empty;
            var source = item.Assignment is null ? "calendar" : "todo";
            return new Text($"{dateLabel} · ALL DAY{estimate} · {item.Title} ({source})",
                themeRenderer.Style(theme.Heading, Decoration.Bold)).Ellipsis();
        }

        if (view.SelectedItem is { } selected)
        {
            var source = selected.ItemType switch
            {
                PlannerItemType.Task => "todo",
                PlannerItemType.Pomodoro => "pomodoro",
                _ => "calendar"
            };
            var focus = selected.ItemType == PlannerItemType.Pomodoro ? view.ActiveFocusBlock : null;
            var duration = focus is null
                ? selected.Assignment?.Todo.Duration ?? selected.Duration
                : focus.EndsAt - focus.StartedAt;
            var start = focus is null ? selected.Start : TimeOnly.FromDateTime(focus.StartedAt);
            var startDate = focus is null ? date : DateOnly.FromDateTime(focus.StartedAt);
            var range = duration is { } span
                ? FormatMultiDayTimeRange(startDate, start, span)
                : start.ToString("HH:mm");
            var durationLabel = duration is { } timed
                ? $" · {(int)timed.TotalMinutes}m"
                : string.Empty;
            return new Text($"{dateLabel} · {range}{durationLabel} · {selected.Title} ({source})",
                themeRenderer.Style(selected.Meeting is null ? theme.Heading : theme.Info, Decoration.Bold)).Ellipsis();
        }

        return new Text($"{dateLabel} · {view.SelectedSlot.Time:HH:mm} · Empty destination",
            themeRenderer.Style(theme.Muted, Decoration.Dim)).Ellipsis();
    }

    private static string FormatMultiDayTimeRange(DateOnly date, TimeOnly start, TimeSpan duration)
    {
        var startAt = date.ToDateTime(start);
        var endAt = startAt.Add(duration);
        var endLabel = endAt.Date == startAt.Date
            ? endAt.ToString("HH:mm")
            : endAt.ToString("ddd dd HH:mm").ToUpperInvariant();
        return $"{start:HH:mm}–{endLabel}";
    }

    public IRenderable CreateContent(IReadOnlyList<IRenderable> lines) =>
        lines.Count == 0 ? new Text(string.Empty) : new Rows(lines);

    public IRenderable PlannerTimeRulerLine(PlannerTimelineRenderRow row, TuiTheme theme, bool selected = false)
    {
        var text = row.IsMinorTimeTick ? row.TimeTickGlyph.PadLeft(5) : row.TimeLabel.PadLeft(5);
        var selectedSlot = selected || (row.IsSelected && row.IsEmpty);
        var line = new Text(
            text,
            themeRenderer.Style(
                selectedSlot ? theme.AccentBright : row.IsMinorTimeTick ? theme.Muted : theme.Date,
                selectedSlot ? Decoration.Bold : row.IsMinorTimeTick ? Decoration.Dim : Decoration.None));
        // Single-day callers apply their existing cell surface themselves.
        // Multiday passes selected explicitly because its shared ruler is a
        // separate cell from the active date pane.
        return selected ? themeRenderer.OnSurface(line, theme.Surface2, true) : line;
    }

    public IRenderable PlannerTimelineRenderLine(PlannerTimelineRenderRow row, TuiTheme theme)
    {
        var selected = row.IsSelected;
        var active = row.IsActive;
        var selectionBridge = row.IsSelectionBridge && !active;
        var completed = row.ItemType == PlannerItemType.Task && row.StatusGlyph == "✓";
        var color = row.ItemType == PlannerItemType.Pomodoro ? theme.Timer :
            active ? theme.AccentBright : completed ? theme.Muted :
            row.ItemType == PlannerItemType.Task ? theme.Text :
            row.ItemType is null ? theme.Muted : theme.Info;
        var decoration = active ? Decoration.Bold : completed || row.IsEmpty ? Decoration.Dim : Decoration.None;
        var line = new System.Text.StringBuilder();
        if (selectionBridge)
        {
            // The overlap stack carries the selected item's vertical branch,
            // but the row itself still belongs to another item. Surface only
            // the junction character; its horizontal branch and content stay
            // in that item's ordinary styling.
            themeRenderer.AppendStyled(
                line,
                row.BranchGlyph[..1],
                theme.AccentBright,
                Decoration.Bold,
                theme.Surface2);
            themeRenderer.AppendStyled(line, row.BranchGlyph[1..], theme.BorderActive, decoration);
        }
        else
        {
            themeRenderer.AppendStyled(
                line,
                row.BranchGlyph,
                active || selected ? theme.AccentBright : row.IsEmpty ? theme.Muted : theme.BorderActive,
                active || selected ? Decoration.Bold : decoration);
        }

        if (row.IsEmpty)
        {
            return new Markup(line.ToString()).Ellipsis();
        }

        themeRenderer.AppendStyled(line, " ", color, decoration);
        if (row.StatusGlyph.Length > 0)
        {
            var glyphColor = row.ItemType == PlannerItemType.Task && !completed
                ? active ? theme.AccentBright : theme.Accent
                : color;
            themeRenderer.AppendStyled(line, row.StatusGlyph + " ", glyphColor, decoration);
            themeRenderer.AppendStyled(line, row.Title, color, decoration);
            if (row.Metadata.Length > 0)
            {
                var metadataColor = row.ItemType == PlannerItemType.Pomodoro
                    ? theme.Timer
                    : active ? theme.AccentBright : theme.Muted;
                themeRenderer.AppendStyled(line, " " + row.Metadata, metadataColor, decoration);
            }
        }

        // Date panes have fixed widths. Ellipsizing here prevents a long task
        // or calendar title from becoming a second physical table row.
        return new Markup(line.ToString()).Ellipsis();
    }

    public IRenderable PlannerMeetingLine(
        PlannerCalendarMeeting meeting,
        string prefix,
        bool selected,
        int additionalMeetings,
        TuiTheme theme)
    {
        var line = new System.Text.StringBuilder();
        var color = selected ? theme.AccentBright : theme.Info;
        var decoration = selected ? Decoration.Bold : Decoration.None;
        themeRenderer.AppendStyled(line, $"{prefix} MEETING ", color, decoration);
        themeRenderer.AppendStyled(line, calendarItemRenderer.MeetingLabel(meeting), color, decoration);
        if (additionalMeetings > 0)
        {
            themeRenderer.AppendStyled(line, $" +{additionalMeetings}", selected ? theme.AccentBright : theme.Warning, decoration);
        }

        return new Markup(line.ToString());
    }

}
