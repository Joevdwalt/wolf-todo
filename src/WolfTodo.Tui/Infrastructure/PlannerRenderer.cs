using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Infrastructure;

public sealed class PlannerRenderer
{
    private readonly BrowserRenderer renderer;

    public PlannerRenderer()
        : this(new BrowserRenderer())
    {
    }

    public PlannerRenderer(
        Func<int> widthProvider,
        Func<int> heightProvider,
        Func<DateOnly>? todayProvider = null,
        Func<DateTime>? nowProvider = null)
        : this(new BrowserRenderer(widthProvider, heightProvider, todayProvider, nowProvider))
    {
    }

    public PlannerRenderer(BrowserRenderer renderer)
    {
        this.renderer = renderer;
    }

    public void ShowPlanner(
        TabStripView tabs,
        PlannerView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme) =>
        renderer.ShowPlanner(tabs, view, keyBindings, theme);
}
