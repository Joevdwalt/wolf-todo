using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerTransition(
    PlannerState State,
    PlannerOperation Operation,
    TodoIdentity? TodoIdentity,
    string? ProjectPath = null,
    TodoUpdate? Update = null,
    TodoContentUpdate? ContentUpdate = null);
