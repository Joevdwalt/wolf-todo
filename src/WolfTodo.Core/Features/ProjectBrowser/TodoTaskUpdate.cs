namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoTaskUpdate(
    TodoUpdate Fields,
    TodoContentUpdate Content);
