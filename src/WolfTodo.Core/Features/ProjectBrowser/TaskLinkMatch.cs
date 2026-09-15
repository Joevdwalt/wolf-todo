namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TaskLinkMatch(
    TodoProject Project,
    TodoItem Todo,
    int? ParentSourceLine);
