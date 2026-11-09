using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoRow(
    string? Heading,
    TodoItem? Todo,
    ImmutableArray<TodoTreeSegment> TreePath,
    bool IsSelected,
    TodoIdentity? Identity = null)
{
    public string? ProjectTitle { get; init; }

    public bool IsMarked { get; init; }

    public bool HasVisibleChildren { get; init; }

    public bool IsCollapsed { get; init; }

    public int Depth => TreePath.Length;
}
