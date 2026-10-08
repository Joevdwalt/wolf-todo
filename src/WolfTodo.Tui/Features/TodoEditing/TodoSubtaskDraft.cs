namespace WolfTodo.Tui.Features.TodoEditing;

public sealed record TodoSubtaskDraft(
    int? SourceLine,
    string Title,
    bool IsCompleted,
    int DescendantCount);
