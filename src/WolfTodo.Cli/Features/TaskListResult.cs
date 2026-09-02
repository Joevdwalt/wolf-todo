using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed record TaskListResult(
    bool Succeeded,
    string? ErrorCode,
    string? Error,
    IReadOnlyList<TodoProject> Projects)
{
    public static TaskListResult Success(IReadOnlyList<TodoProject> projects) => new(true, null, null, projects);

    public static TaskListResult Failure(string code, string error) => new(false, code, error, []);
}
