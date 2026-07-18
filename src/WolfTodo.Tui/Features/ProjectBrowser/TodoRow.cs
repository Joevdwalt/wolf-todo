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

    public int Depth => TreePath.Length;
}

public enum TodoTreeSegment
{
    HasFollowingSibling,
    LastSibling
}

public static class TodoTreeFormatter
{
    public static string Format(ImmutableArray<TodoTreeSegment> path)
    {
        if (path.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var prefix = new System.Text.StringBuilder();
        foreach (var segment in path[..^1])
        {
            prefix.Append(segment == TodoTreeSegment.HasFollowingSibling ? "│  " : "   ");
        }

        prefix.Append(path[^1] == TodoTreeSegment.HasFollowingSibling ? "├─ " : "└─ ");
        return prefix.ToString();
    }

    public static string FormatContinuation(ImmutableArray<TodoTreeSegment> path)
    {
        if (path.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        return string.Concat(path.Select(segment =>
            segment == TodoTreeSegment.HasFollowingSibling ? "│  " : "   "));
    }
}
