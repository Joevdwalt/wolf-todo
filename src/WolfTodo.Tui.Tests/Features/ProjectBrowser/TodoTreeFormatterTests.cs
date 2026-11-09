using FluentAssertions;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class TodoTreeFormatterTests
{
    [Fact]
    public void Format_preserves_the_default_final_branch()
    {
        TodoTreeFormatter.Format([TodoTreeSegment.HasFollowingSibling, TodoTreeSegment.LastSibling])
            .Should().Be("│  └─ ");
    }

    [Theory]
    [InlineData(TodoTreeSegment.HasFollowingSibling, TodoTreeSegment.HasFollowingSibling, "│  ├─ ")]
    [InlineData(TodoTreeSegment.HasFollowingSibling, TodoTreeSegment.LastSibling, "│  ╰─ ")]
    [InlineData(TodoTreeSegment.LastSibling, TodoTreeSegment.LastSibling, "   ╰─ ")]
    public void Format_supports_rounded_final_branches_and_ancestor_guides(
        TodoTreeSegment ancestor, TodoTreeSegment item, string expected)
    {
        TodoTreeFormatter.Format([ancestor, item], endBranchGlyph: "╰─").Should().Be(expected);
    }

    [Fact]
    public void Root_paths_have_no_branch_or_continuation()
    {
        TodoTreeFormatter.Format(default, "╰─").Should().BeEmpty();
        TodoTreeFormatter.Format([], "╰─").Should().BeEmpty();
        TodoTreeFormatter.FormatContinuation(default).Should().BeEmpty();
        TodoTreeFormatter.FormatContinuation([]).Should().BeEmpty();
    }

    [Fact]
    public void Continuation_preserves_each_following_sibling_and_spaces_after_final_ancestors()
    {
        TodoTreeFormatter.FormatContinuation(
            [TodoTreeSegment.HasFollowingSibling, TodoTreeSegment.LastSibling, TodoTreeSegment.HasFollowingSibling])
            .Should().Be("│     │  ");
    }
}
