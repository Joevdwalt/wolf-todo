using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationState(TabHostState Tabs, BrowserState Browser)
{
    public PlannerState Planner { get; init; } = PlannerState.CreateInitial(
        DateOnly.FromDateTime(DateTime.Today));

    public static ApplicationState CreateInitial(TabHostState tabs) => new(tabs, BrowserState.Initial);
}
