using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoRow(
    string? Heading,
    TodoItem? Todo,
    int Depth,
    bool IsSelected,
    TodoIdentity? Identity = null)
{
    public string? ProjectTitle { get; init; }
}
