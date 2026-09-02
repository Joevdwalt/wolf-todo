using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed record TaskImportResult(
    bool Succeeded,
    string? ErrorCode,
    string? Error,
    string? ProjectTitle,
    string? ProjectPath,
    IReadOnlyList<int> SourceLines)
{
    public static TaskImportResult Success(TodoProject project, IReadOnlyList<int> sourceLines) =>
        new(true, null, null, project.Title, project.Path, sourceLines);

    public static TaskImportResult Failure(string code, string error) =>
        new(false, code, error, null, null, []);
}
