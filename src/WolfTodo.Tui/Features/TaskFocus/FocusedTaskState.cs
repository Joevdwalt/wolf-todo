using WolfTodo.Tui.Features.TodoEditing;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.TaskFocus;

public sealed record FocusedTaskState(
    TodoIdentity RootIdentity,
    TodoItem RootSnapshot,
    TodoIdentity SelectedIdentity)
{
    public TodoTaskEditorState? Editor { get; init; }

    public string? Error { get; init; }

    public string? StatusMessage { get; init; }

    public static FocusedTaskState Create(TodoIdentity identity, TodoItem todo) =>
        new(identity, todo, identity);
}
