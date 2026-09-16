using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record FocusedTaskItem(
    TodoIdentity Identity,
    TodoItem Todo,
    ImmutableArray<TodoTreeSegment> TreePath,
    bool IsSelected)
{
    public bool IsRoot => TreePath.IsDefaultOrEmpty;
}
