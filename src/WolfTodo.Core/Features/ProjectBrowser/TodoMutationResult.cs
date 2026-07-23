namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoMutationResult(bool Succeeded, int? SourceLine, string? Error)
{
    public static TodoMutationResult Success() => new(true, null, null);

    public static TodoMutationResult Success(int sourceLine) => new(true, sourceLine, null);

    public static TodoMutationResult Failure(string error) => new(false, null, error);
}
