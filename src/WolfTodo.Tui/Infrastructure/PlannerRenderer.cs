using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Infrastructure;

public sealed class PlannerRenderer
{
    private readonly Func<int> widthProvider;
    private readonly Func<int> heightProvider;
    private readonly Func<DateTime> nowProvider;

    public PlannerRenderer()
        : this(BrowserRenderer.SafeWindowWidth, BrowserRenderer.SafeWindowHeight, null, null)
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
        BrowserRenderer.RenderPlannerHeader(tabs, view, keyBindings, theme, context);

        var timelineRows = BrowserRenderer.WindowPlannerTimeline(
            view.Slots,
            view.State.SlotIndex,
            context.AvailableRows,
            view.State.SelectedDate,
            nowProvider());
        var timelineTable = BrowserRenderer.CreatePlannerTimelineTable(timelineRows, context.AvailableRows, theme);

        BrowserRenderer.RenderPlannerBody(view, theme, context, timelineTable);
        BrowserRenderer.RenderPlannerOverlay(view, keyBindings, theme, context);
        BrowserRenderer.WritePlannerStatus(context.Status, view, theme, context.EditorDialog);
    }

    public PlannerRenderContext CreatePlannerRenderContext(
        PlannerView view,
        TuiKeyBindings keyBindings)
    {
        var width = widthProvider();
        var height = heightProvider();
        var selectRows = BrowserRenderer.SelectListRows(height);
        var textBoxRows = BrowserRenderer.TextBoxRows(height);
        var selectList = BrowserRenderer.PlannerSelectList(view, keyBindings);
        var textBox = BrowserRenderer.PlannerTextBox(view);
        var editorDialog = BrowserRenderer.CreatePlannerEditorDialog(view, keyBindings, width, height);
        var status = BrowserRenderer.PlannerStatus(view, keyBindings, width, height);
        var wideLayout = width >= 120;
        var allDayVisible = view.CalendarAgenda.AllDayItems.Length > 0 ||
                            view.State.Focus == PlannerFocus.AllDay;
        var showAllDayPanel = allDayVisible || (wideLayout && view.State.ShowDetails);
        var wideSidePanels = wideLayout && (view.State.ShowDetails || showAllDayPanel);
        var compactDetails = BrowserRenderer.IsPlannerCompactDetailsVisible(view, wideSidePanels);
        var narrowAllDayHeight = BrowserRenderer.PlannerNarrowAllDayHeight(view, wideSidePanels, showAllDayPanel);
        var pickerHeight = BrowserRenderer.PlannerPickerHeight(selectList, width, selectRows, textBox, textBoxRows);
        var availableRows = BrowserRenderer.PlannerAvailableRows(
            height,
            BrowserRenderer.DialogContentHeight(editorDialog) ?? status.Count,
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
            editorDialog,
            status,
            wideSidePanels,
            showAllDayPanel,
            compactDetails,
            narrowAllDayHeight,
            availableRows,
            timelineWidth);
    }
}
