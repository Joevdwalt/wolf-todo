using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record ProjectCatalog(
    ImmutableArray<TodoProject> Projects,
    ImmutableArray<ProjectSourceError> Errors);
