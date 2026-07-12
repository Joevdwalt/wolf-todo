namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record ProjectParseResult(TodoProject? Project, string? Error)
{
    public bool IsSuccess => Project is not null;

    public static ProjectParseResult Success(TodoProject project) => new(project, null);

    public static ProjectParseResult Failure(string error) => new(null, error);
}
