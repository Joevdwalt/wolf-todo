using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Rendering;

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
            prefix.Append(segment == TodoTreeSegment.HasFollowingSibling ? $"{TodoGlyphs.TreeContinuationGlyph}  " : "   ");
        }

        prefix.Append(path[^1] == TodoTreeSegment.HasFollowingSibling
            ? $"{TodoGlyphs.TreeBranchGlyph} "
            : $"{TodoGlyphs.TreeEndBranchGlyph} ");
        return prefix.ToString();
    }

    public static string FormatContinuation(ImmutableArray<TodoTreeSegment> path)
    {
        if (path.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        return string.Concat(path.Select(segment =>
            segment == TodoTreeSegment.HasFollowingSibling ? $"{TodoGlyphs.TreeContinuationGlyph}  " : "   "));
    }
}
