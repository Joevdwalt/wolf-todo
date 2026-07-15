using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoUpdate(
    string Title,
    string? ExternalReference,
    TodoPriority? Priority,
    ImmutableArray<string> Tags,
    DateOnly? StartDate,
    DateOnly? DueDate);
