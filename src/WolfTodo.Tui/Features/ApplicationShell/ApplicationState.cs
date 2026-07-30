using WolfTodo.Tui.Features.ApplicationShell.CommandPalette;
using WolfTodo.Tui.Features.ApplicationShell.Commands;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationState(TabHostState Tabs, BrowserState Browser)
{
    public ApplicationCommandState Command { get; init; } = ApplicationCommandState.Initial;

    public CommandPaletteState Palette { get; init; } = CommandPaletteState.Closed;

    public PlannerState Planner { get; init; } = PlannerState.CreateInitial(
        DateOnly.FromDateTime(DateTime.Today));

    public static ApplicationState CreateInitial(TabHostState tabs) => new(tabs, BrowserState.Initial);
}
