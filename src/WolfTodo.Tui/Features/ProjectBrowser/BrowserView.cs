using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record BrowserView(
    BrowserState State,
    ImmutableArray<ProjectRow> Projects,
    ImmutableArray<TodoRow> Todos,
    TodoItem? SelectedTodo,
    string SelectedProjectTitle,
    string? SelectedProjectPath,
    string? Diagnostic,
    string EmptyMessage)
{
    public int SelectableTodoCount => Todos.Count(row => row.Todo is not null);
}
