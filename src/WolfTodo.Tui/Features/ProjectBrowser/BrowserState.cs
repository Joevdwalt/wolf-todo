using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record BrowserState(
    BrowserFocus Focus,
    int ProjectIndex,
    int TodoIndex,
    bool ShowCompleted,
    bool IsFilterMode,
    string FilterText,
    string FilterDraft,
    bool IsSortMode,
    TodoSort Sort,
    TodoIdentity? PendingTodoSelection,
    string? Error)
{
    public TodoTaskEditorState? Editor { get; init; }

    public TodoBulkEditorState? BulkEditor { get; init; }

    public ImmutableHashSet<TodoIdentity> MarkedTodos { get; init; } = [];

    public ImmutableDictionary<TodoIdentity, TodoItem> MarkedTodoSnapshots { get; init; } =
        ImmutableDictionary<TodoIdentity, TodoItem>.Empty;

    public string? StatusMessage { get; init; }

    public bool ShowDetails { get; init; } = true;

    public static BrowserState Initial { get; } = new(
        BrowserFocus.Projects,
        0,
        0,
        false,
        false,
        string.Empty,
        string.Empty,
        false,
        TodoSort.Source,
        null,
        null);
}
