using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoBatchMutationResult(
    bool Succeeded,
    ImmutableArray<int> SourceLines,
    string? Error)
{
    public static TodoBatchMutationResult Success(IEnumerable<int> sourceLines) =>
        new(true, [.. sourceLines], null);

    public static TodoBatchMutationResult Failure(string error) =>
        new(false, [], error);
}
