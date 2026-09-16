using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record FocusedTaskTransition(
    FocusedTaskState State,
    FocusedTaskOperation Operation = FocusedTaskOperation.None,
    TodoIdentity? Identity = null,
    TodoTaskUpdate? Update = null,
    TodoItem? ExpectedTodo = null);
