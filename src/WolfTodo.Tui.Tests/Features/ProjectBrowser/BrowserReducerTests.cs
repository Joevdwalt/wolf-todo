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
}
