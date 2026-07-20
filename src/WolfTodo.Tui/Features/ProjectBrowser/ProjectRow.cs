using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record ProjectRow(
    string Title,
    int ActiveCount,
    TodoProject? Project,
    ProjectSourceError? Error,
    bool IsSelected,
    ProjectRowKind Kind = ProjectRowKind.Project);

public enum ProjectRowKind
{
    All,
    Today,
    Project,
    Error
}
