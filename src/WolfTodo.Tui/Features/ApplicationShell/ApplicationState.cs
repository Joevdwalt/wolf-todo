using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationState(TabHostState Tabs, BrowserState Browser)
{
    public ApplicationCommandState Command { get; init; } = ApplicationCommandState.Initial;

    public CommandPaletteState Palette { get; init; } = CommandPaletteState.Closed;

    public PlannerState Planner { get; init; } = PlannerState.CreateInitial(
        DateOnly.FromDateTime(DateTime.Today));

    public ActiveTimer? Timer { get; init; }

    public PomodoroPromptState? PomodoroPrompt { get; init; }

    public PomodoroCompletion? PomodoroCompletion { get; init; }

    public static ApplicationState CreateInitial(TabHostState tabs) => new(tabs, BrowserState.Initial);
}
