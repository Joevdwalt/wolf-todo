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
    public TodoFormState? Form { get; init; }

    public TodoContentEditorState? ContentEditor { get; init; }

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
