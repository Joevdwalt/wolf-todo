using System.Collections.Immutable;
using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class BrowserReducerTests
{
    private readonly BrowserReducer reducer = new();
    private static readonly ApplicationConfiguration Configuration = new([], ":q");

    [Fact]
    public void Reduce_moves_from_projects_to_todos_on_enter()
    {
        var result = reducer.Reduce(BrowserState.Initial, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.Focus.Should().Be(BrowserFocus.Todos);
    }

    [Fact]
    public void Reduce_toggles_completed_todos_from_command_mode()
    {
        var state = BrowserState.Initial with { IsCommandMode = true, Command = ":completed" };

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.ShowCompleted.Should().BeTrue();
        result.State.IsCommandMode.Should().BeFalse();
    }

    [Fact]
    public void Reduce_exits_for_the_configured_quit_command()
    {
        var state = BrowserState.Initial with { IsCommandMode = true, Command = ":q" };

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.ShouldExit.Should().BeTrue();
    }

    [Fact]
    public void Reduce_reports_an_unknown_command()
    {
        var state = BrowserState.Initial with { IsCommandMode = true, Command = ":wat" };

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.Error.Should().Be("Unknown command: :wat");
    }

    [Fact]
    public void Reduce_opens_filter_mode_with_the_committed_filter()
    {
        var state = BrowserState.Initial with { FilterText = "renewal" };

        var result = reducer.Reduce(state, Key('/'), Configuration, EmptyView());

        result.State.IsFilterMode.Should().BeTrue();
        result.State.FilterDraft.Should().Be("renewal");
    }

    [Fact]
    public void Reduce_updates_the_filter_draft_and_resets_todo_selection_while_typing()
    {
        var state = BrowserState.Initial with
        {
            IsFilterMode = true,
            FilterDraft = "rene",
            TodoIndex = 3
        };

        var result = reducer.Reduce(state, Key('w'), Configuration, EmptyView());

        result.State.FilterDraft.Should().Be("renew");
        result.State.TodoIndex.Should().Be(0);
    }

    [Fact]
    public void Reduce_commits_a_trimmed_filter_on_enter()
    {
        var state = BrowserState.Initial with { IsFilterMode = true, FilterDraft = "  renewal  " };

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.IsFilterMode.Should().BeFalse();
        result.State.FilterText.Should().Be("renewal");
        result.State.FilterDraft.Should().Be("renewal");
    }

    [Fact]
    public void Reduce_clears_the_filter_when_an_empty_draft_is_submitted()
    {
        var state = BrowserState.Initial with
        {
            IsFilterMode = true,
            FilterText = "renewal",
            FilterDraft = string.Empty
        };

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.FilterText.Should().BeEmpty();
    }

    [Fact]
    public void Reduce_restores_the_committed_filter_when_filter_editing_is_cancelled()
    {
        var state = BrowserState.Initial with
        {
            IsFilterMode = true,
            FilterText = "renewal",
            FilterDraft = "replacement"
        };

        var result = reducer.Reduce(state, Key(ConsoleKey.Escape), Configuration, EmptyView());

        result.State.IsFilterMode.Should().BeFalse();
        result.State.FilterText.Should().Be("renewal");
        result.State.FilterDraft.Should().Be("renewal");
    }

    [Fact]
    public void Reduce_treats_slash_as_command_text_while_in_command_mode()
    {
        var state = BrowserState.Initial with { IsCommandMode = true, Command = ":" };

        var result = reducer.Reduce(state, Key('/'), Configuration, EmptyView());

        result.State.Command.Should().Be(":/");
        result.State.IsFilterMode.Should().BeFalse();
    }

    private static BrowserView EmptyView() => new(
        BrowserState.Initial,
        [new ProjectRow("All", 0, null, null, true)],
        ImmutableArray<TodoRow>.Empty,
        null,
        "All",
        null,
        null,
        "No projects found");

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
