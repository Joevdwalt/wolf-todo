using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed record ProjectTargetResolution(TodoProject? Project, string? ErrorCode, string? Error)
{
    public static ProjectTargetResolution Success(TodoProject project) => new(project, null, null);

    public static ProjectTargetResolution Failure(string code, string error) => new(null, code, error);
}
