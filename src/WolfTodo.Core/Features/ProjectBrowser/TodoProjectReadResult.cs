namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoProjectReadResult(string Path, TodoProject? Project, string? Error)
{
    public bool IsSuccess => Project is not null;

    public static TodoProjectReadResult Success(string path, TodoProject project) => new(path, project, null);

    public static TodoProjectReadResult Failure(string path, string error) => new(path, null, error);
}
