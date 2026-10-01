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
    private readonly PlannerOverlayViewFactory overlayViews;
    private readonly CalendarItemRenderer calendarItemRenderer = new();
    private readonly PlannerTimelineViewport viewport = new();
    private readonly PlannerDetailRenderer detailRenderer = new();
    private readonly PlannerSingleDayTimelineRenderer singleDayTimelineRenderer;
    private readonly PlannerMultiDayTimelineRenderer multiDayTimelineRenderer;

    public PlannerRenderer()
        : this(TerminalLayout.SafeWindowWidth, TerminalLayout.SafeWindowHeight, null)
    {
    }

    public PlannerRenderer(
        Func<int> widthProvider,
        Func<int> heightProvider,
        Func<DateTime>? nowProvider = null)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
        this.nowProvider = nowProvider ?? (() => DateTime.Now);
        overlayViews = new PlannerOverlayViewFactory(statusRenderer);
        var lineRenderer = new PlannerTimelineLineRenderer();
        singleDayTimelineRenderer = new PlannerSingleDayTimelineRenderer(lineRenderer, viewport);
        var cellRenderer = new PlannerMultiDayCellRenderer(lineRenderer);
        multiDayTimelineRenderer = new PlannerMultiDayTimelineRenderer(
            widthProvider, this.nowProvider, lineRenderer, viewport, cellRenderer);
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
            ? multiDayTimelineRenderer.CreatePlannerMultiDayTimelineTable(view, context.AvailableRows, theme)
            : singleDayTimelineRenderer.CreatePlannerTimelineTable(
                viewport.WindowPlannerTimeline(
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
        var selectList = overlayViews.PlannerSelectList(view, keyBindings);
        var textBox = overlayViews.PlannerTextBox(view);
        var editorDialog = overlayViews.CreatePlannerEditorDialog(view, keyBindings, width, height);
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
                    CreateContent(detailRenderer.PlannerDetailLines(view, theme)),
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
                new Panel(detailRenderer.PlannerCompactDetail(view, theme))
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

    public IRenderable CreateContent(IReadOnlyList<IRenderable> lines) =>
        lines.Count == 0 ? new Text(string.Empty) : new Rows(lines);


}
