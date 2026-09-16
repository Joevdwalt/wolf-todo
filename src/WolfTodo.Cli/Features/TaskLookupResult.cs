using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed record TaskLookupResult(
    bool Succeeded,
    string? ErrorCode,
    string? Error,
    TaskLinkMatch? Match)
{
    public static TaskLookupResult Success(TaskLinkMatch match) => new(true, null, null, match);

    public static TaskLookupResult Failure(string code, string error) => new(false, code, error, null);
}
