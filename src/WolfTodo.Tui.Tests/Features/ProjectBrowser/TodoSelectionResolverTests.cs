using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class TodoSelectionResolverTests
{
    private static TodoIdentity Identity(int line) => new("/work.md", line);

    private static TodoRow Row(int line) => new(null,
        new TodoItem(line, false, null, $"Task {line}", null, [], null, null, "", [], []),
        [], false, Identity(line));

    private static BrowserView View(bool showCompleted, params int[] lines) => new(
        BrowserState.Initial with { ShowCompleted = showCompleted }, [],
        [.. lines.Select(Row)], null, "Work", "/work.md", null, "");

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void AfterCompletion_selects_previous_or_next_visible_task(int completed, int expected)
    {
        var remaining = new[] { 1, 2, 3 }.Where(line => line != completed).ToArray();
        TodoSelectionResolver.AfterCompletion(View(false, 1, 2, 3), View(false, remaining), Identity(completed))
            .Should().Be(Identity(expected));
    }

    [Fact]
    public void AfterCompletion_keeps_visible_completed_task_even_when_reordered()
    {
        TodoSelectionResolver.AfterCompletion(View(true, 1, 2, 3), View(true, 1, 3, 2), Identity(2))
            .Should().Be(Identity(2));
    }

    [Fact]
    public void AfterCompletion_returns_null_for_empty_view()
    {
        TodoSelectionResolver.AfterCompletion(View(false, 1), View(false), Identity(1)).Should().BeNull();
    }

    [Fact]
    public void AfterCompletion_skips_disappeared_neighbors_and_uses_newly_visible_rows()
    {
        TodoSelectionResolver.AfterCompletion(View(false, 1, 2, 3), View(false, 3), Identity(2))
            .Should().Be(Identity(3));
        TodoSelectionResolver.AfterCompletion(View(false, 1), View(false, 2), Identity(1))
            .Should().Be(Identity(2));
    }

    [Fact]
    public void ResolveIndex_follows_visible_ancestors_and_clamps_fallback_index()
    {
        var rows = new[] { Row(1), Row(2) };
        var ancestors = new Dictionary<TodoIdentity, TodoIdentity> { [Identity(4)] = Identity(3), [Identity(3)] = Identity(2) };
        TodoSelectionResolver.ResolveIndex(rows, BrowserState.Initial with { PendingTodoSelection = Identity(4) }, ancestors)
            .Should().Be(1);
        TodoSelectionResolver.ResolveIndex(rows, BrowserState.Initial with { TodoIndex = 8 }, ancestors)
            .Should().Be(1);
        TodoSelectionResolver.ResolveIndex([], BrowserState.Initial, ancestors).Should().Be(0);
    }
}
