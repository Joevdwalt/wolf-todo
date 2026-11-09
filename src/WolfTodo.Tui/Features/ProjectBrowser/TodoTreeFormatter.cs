using System.Collections.Immutable;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public static class TodoTreeFormatter
{
    public static string Format(
        ImmutableArray<TodoTreeSegment> path,
        string endBranchGlyph = TodoGlyphs.TreeEndBranchGlyph)
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
            : $"{endBranchGlyph} ");
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
