using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoProject(
    string Title,
    string Path,
    ImmutableArray<TodoItem> Todos);
