using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerAssignment(
    TodoIdentity Identity,
    string ProjectTitle,
    string ProjectPath,
    TodoItem Todo);
