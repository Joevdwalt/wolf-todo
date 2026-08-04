using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Infrastructure;

public sealed class StatusRenderer
{
    public IReadOnlyList<BrowserStatusLine> BrowserStatus(
        BrowserView view,
        TuiKeyBindings keyBindings,
        bool compact,
        int terminalWidth,
        int terminalHeight) =>
        BrowserRenderer.CreateStatusLines(view, keyBindings, compact, terminalWidth, terminalHeight);

    public IReadOnlyList<BrowserStatusLine> PlannerStatus(
        PlannerView view,
        TuiKeyBindings keyBindings,
        int terminalWidth,
        int terminalHeight) =>
        BrowserRenderer.PlannerStatus(view, keyBindings, terminalWidth, terminalHeight);

    public string BrowserMode(BrowserView view) =>
        BrowserRenderer.BrowserMode(view);

    public string PlannerMode(PlannerView view) =>
        BrowserRenderer.PlannerModeLabel(view);

    public string SortHint(BrowserState state, TuiKeyBindings bindings) =>
        BrowserRenderer.SortHint(state, bindings);

    public IReadOnlyList<string> Wrap(string value, int width) =>
        BrowserRenderer.WrapStatus(value, width);

    public string CommandPaletteFooter(TuiKeyBindings bindings) =>
        BrowserRenderer.CommandPaletteFooter(bindings);

    public string PlannerPickerFooter(TuiKeyBindings bindings) =>
        BrowserRenderer.PlannerPickerFooter(bindings);

    public string CalendarStatus(PlannerCalendarAgenda agenda) =>
        BrowserRenderer.CalendarStatus(agenda);
}
