using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoItem(
    int SourceLine,
    bool IsCompleted,
    string? ExternalReference,
    string Title,
    TodoPriority? Priority,
    ImmutableArray<string> Tags,
    DateOnly? StartDate,
    DateOnly? DueDate,
    string SectionPath,
    ImmutableArray<TodoNote> Notes,
    ImmutableArray<TodoItem> Subtasks)
{
    public TodoSchedule? Schedule { get; init; }
}
