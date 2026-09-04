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
        RenderPlannerHeader(tabs, view, keyBindings, theme, context);

        var timelineRows = WindowPlannerTimeline(
            view.Slots,
            view.State.SlotIndex,
            context.AvailableRows,
            view.State.SelectedDate,
            nowProvider(),
            view.CalendarAgenda.Meetings,
            view.ActiveFocusBlock);
        var timelineTable = CreatePlannerTimelineTable(timelineRows, context.AvailableRows, theme);

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
        var allDayVisible = view.CalendarAgenda.AllDayItems.Length > 0 ||
                            view.State.Focus == PlannerFocus.AllDay;
        var showAllDayPanel = allDayVisible || (wideLayout && view.State.ShowDetails);
        var wideSidePanels = wideLayout && (view.State.ShowDetails || showAllDayPanel);
        var compactDetails = IsPlannerCompactDetailsVisible(view, wideSidePanels);
        var narrowAllDayHeight = PlannerNarrowAllDayHeight(view, wideSidePanels, showAllDayPanel);
        var pickerHeight = TerminalLayout.PickerHeight(selectList, width, selectRows, textBox, textBoxRows);
        pickerHeight += view.PomodoroPrompt is null ? 0 : PomodoroPromptRenderer.Height;
        var availableRows = PlannerAvailableRows(
            height,
            TerminalLayout.DialogContentHeight(editorDialog) ?? status.Count,
            pickerHeight,
            compactDetails,
            narrowAllDayHeight);
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
        PlannerRenderContext context) =>
        WriteOperationalHeader(
            tabs,
            keyBindings,
            theme,
            context.Width,
            statusRenderer.PlannerMode(view),
            view.State.SelectedDate,
            view.OpenTodoCount,
            view.ProjectErrorCount);

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
        const int inspectorContentHeight = 10;
        var allDayContentHeight = Math.Max(
            1,
            context.AvailableRows - (view.State.ShowDetails ? inspectorContentHeight + 2 : 0));
        var sidePanels = new List<IRenderable>();
        if (view.State.ShowDetails)
        {
            sidePanels.Add(PlannerPanel(
                "INSPECTOR",
                FixedLines(PlannerDetailLines(view, theme), inspectorContentHeight),
                theme));
        }

        if (context.ShowAllDayPanel)
        {
            sidePanels.Add(PlannerPanel(
                "ALL DAY",
                calendarItemRenderer.AllDayAgendaLines(view, theme, allDayContentHeight),
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
                    calendarItemRenderer.AllDayAgendaLines(view, theme, Math.Max(1, context.NarrowAllDayHeight - 2)),
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

        if (editor.Mode != TodoTaskEditorMode.ChooseContentType)
        {
            return null;
        }

        return new SelectListView(
            "Add content",
            Enum.GetValues<ContentItemKind>()
                .Select(kind => new SelectOption(kind.ToString().ToUpperInvariant()))
                .ToArray(),
            (int)editor.AddKind,
            null,
            "No content types available",
            $"{statusRenderer.Shortest(bindings.MoveDown)}/{statusRenderer.Shortest(bindings.MoveUp)} MOVE  " +
            $"{statusRenderer.Shortest(bindings.Open)} SELECT  {statusRenderer.Shortest(bindings.Back)} CANCEL",
            editor.Error);
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
        IReadOnlyList<IRenderable> lines,
        TuiTheme theme,
        bool active = false)
    {
        var styledHeader = new System.Text.StringBuilder();
        themeRenderer.AppendStyled(styledHeader, header, theme.AccentBright, Decoration.Bold);
        return new Panel(CreateContent(lines))
        {
            Header = new PanelHeader(styledHeader.ToString()),
            Border = BoxBorder.Square,
            BorderStyle = themeRenderer.Style(active ? theme.AccentBright : theme.BorderActive),
            Expand = true
        };
    }

    public IReadOnlyList<IRenderable> FixedLines(
        IReadOnlyList<IRenderable> lines,
        int contentHeight)
    {
        var fitted = calendarItemRenderer.FitLines(lines, contentHeight, 0).ToList();
        while (fitted.Count < contentHeight)
        {
            fitted.Add(new Text(string.Empty));
        }

        return fitted;
    }

    public IRenderable PlannerCompactDetail(PlannerView view, TuiTheme theme)
    {
        if (view.State.Focus == PlannerFocus.AllDay)
        {
            var item = view.SelectedAllDayItem;
            if (item is null)
            {
                return new Text("Empty all-day schedule", themeRenderer.Style(theme.Muted, Decoration.Dim));
            }

            var label = item.Assignment is null
                ? $"{item.Title}  ·  {calendarItemRenderer.AllDayKindLabel(item.Kind)}  ·  READ ONLY"
                : $"{item.Title}  ·  {item.ProjectTitle}  ·  ALL DAY";
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

    public IRenderable CreateContent(IReadOnlyList<IRenderable> lines) =>
        lines.Count == 0 ? new Text(string.Empty) : new Rows(lines);

    public IRenderable PlannerTimeRulerLine(PlannerTimelineRenderRow row, TuiTheme theme)
    {
        var text = row.IsMinorTimeTick ? row.TimeTickGlyph.PadLeft(5) : row.TimeLabel.PadLeft(5);
        var selectedEmptySlot = row.IsSelected && row.IsEmpty;
        return new Text(
            text,
            themeRenderer.Style(
                selectedEmptySlot ? theme.AccentBright : row.IsMinorTimeTick ? theme.Muted : theme.Date,
                selectedEmptySlot ? Decoration.Bold : row.IsMinorTimeTick ? Decoration.Dim : Decoration.None));
    }

    public IRenderable PlannerTimelineRenderLine(PlannerTimelineRenderRow row, TuiTheme theme)
    {
        var selected = row.IsSelected;
        var active = row.IsActive;
        var completed = row.ItemType == PlannerItemType.Task && row.StatusGlyph == "✓";
        var color = row.ItemType == PlannerItemType.Pomodoro ? theme.Timer :
            active ? theme.AccentBright : completed ? theme.Muted :
            row.ItemType == PlannerItemType.Task ? theme.Text :
            row.ItemType is null ? theme.Muted : theme.Info;
        var decoration = active ? Decoration.Bold : completed || row.IsEmpty ? Decoration.Dim : Decoration.None;
        var line = new System.Text.StringBuilder();
        themeRenderer.AppendStyled(
            line,
            row.BranchGlyph,
            active || selected ? theme.AccentBright : row.IsEmpty ? theme.Muted : theme.BorderActive,
            active || selected ? Decoration.Bold : decoration);

        if (row.IsEmpty)
        {
            return new Markup(line.ToString());
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

        return new Markup(line.ToString());
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

    public void WriteOperationalHeader(
        TabStripView view,
        TuiKeyBindings bindings,
        TuiTheme theme,
        int terminalWidth,
        string mode,
        DateOnly date,
        int openCount,
        int errorCount)
    {
        var segments = new List<(string Text, Color Color, Decoration Decoration)>();
        if (terminalWidth >= 60)
        {
            segments.Add(("WOLF TODO // ", theme.Heading, Decoration.Bold));
            for (var index = 0; index < view.Tabs.Length; index++)
            {
                if (index > 0)
                {
                    segments.Add(("  ", theme.Text, Decoration.None));
                }

                var tab = view.Tabs[index];
                var title = tab.IsSelected
                    ? $"[{tab.Title.ToUpperInvariant()}]"
                    : tab.Title.ToUpperInvariant();
                var color = tab.IsSelected ? theme.Accent : theme.Muted;
                var decoration = tab.IsSelected ? Decoration.Bold : Decoration.Dim;
                segments.Add((title, color, decoration));
            }
        }
        else
        {
            var active = view.Tabs.First(tab => tab.IsSelected);
            segments.Add(($"[{active.Title.ToUpperInvariant()}]", theme.Accent, Decoration.Bold));
        }

        segments.Add(($"  MODE:{mode}", theme.SecondaryText, Decoration.None));
        if (terminalWidth >= 80)
        {
            segments.Add(($"  {date.ToString("ddd dd MMM").ToUpperInvariant()}", theme.Date, Decoration.None));
        }

        if (terminalWidth >= 100)
        {
            segments.Add(($"  OPEN:{openCount}", theme.Text, Decoration.None));
            segments.Add((
                errorCount == 0 ? "  FILES:CLEAN" : $"  FILES:{errorCount} ERRORS",
                errorCount == 0 ? theme.Muted : theme.Error,
                errorCount == 0 ? Decoration.Dim : Decoration.Bold));
        }

        if (terminalWidth >= 120 && view.Tabs.Length > 1)
        {
            var hint = $"  {TuiKeyBindings.ShortestDisplayName(bindings.TabPrevious)}/" +
                       $"{TuiKeyBindings.ShortestDisplayName(bindings.TabNext)} TABS";
            segments.Add((hint, theme.Muted, Decoration.Dim));
        }

        var totalLength = segments.Sum(segment => segment.Text.Length);
        var width = Math.Max(1, terminalWidth);
        var remaining = totalLength > width ? width - 1 : width;
        var output = new System.Text.StringBuilder();

        foreach (var segment in segments)
        {
            var length = Math.Min(segment.Text.Length, remaining);
            if (length == 0)
            {
                break;
            }

            themeRenderer.AppendStyled(output, segment.Text[..length], segment.Color, segment.Decoration);
            remaining -= length;
        }

        if (totalLength > width)
        {
            themeRenderer.AppendStyled(output, "…", theme.Muted);
        }

        themeRenderer.WriteSurface(new Markup(output.ToString()), theme.Background, true);
        AnsiConsole.WriteLine();
    }
}
