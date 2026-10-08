using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.TodoEditing;

public sealed record TodoEditorTransition(
    TodoTaskEditorState? State,
    TodoEditorOperation Operation = TodoEditorOperation.None,
    string? ProjectPath = null,
    TodoIdentity? Target = null,
    TodoTaskUpdate? Update = null);
