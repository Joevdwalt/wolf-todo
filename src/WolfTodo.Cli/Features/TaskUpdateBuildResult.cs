using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed record TaskUpdateBuildResult(
    TodoTaskUpdate? Value,
    string? ErrorCode,
    string? Error)
{
    public static TaskUpdateBuildResult Success(TodoTaskUpdate value) => new(value, null, null);

    public static TaskUpdateBuildResult Failure(string code, string error) =>
        new(null, code, error);
}
