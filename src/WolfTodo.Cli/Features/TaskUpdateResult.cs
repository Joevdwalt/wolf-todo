using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed record TaskUpdateResult(
    bool Succeeded,
    string? ErrorCode,
    string? Error,
    TaskLinkMatch? Match,
    bool Changed)
{
    public static TaskUpdateResult Success(TaskLinkMatch match, bool changed) =>
        new(true, null, null, match, changed);

    public static TaskUpdateResult Failure(string code, string error) =>
        new(false, code, error, null, false);
}
