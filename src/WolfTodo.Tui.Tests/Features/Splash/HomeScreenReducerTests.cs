using FluentAssertions;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Tests.Features.Splash;

public sealed class HomeScreenReducerTests
{
    private readonly HomeScreenReducer reducer = new();
    private static readonly Keybindings Keybindings = new(":q");

    [Fact]
    public void Reduce_starts_command_mode_when_colon_is_pressed()
    {
        var result = reducer.Reduce(HomeScreenState.Initial, Key(':'), Keybindings);

        result.State.Should().Be(new HomeScreenState(true, ":", null));
        result.ShouldExit.Should().BeFalse();
    }

    [Fact]
    public void Reduce_exits_when_the_submitted_command_matches_quit()
    {
        var result = reducer.Reduce(new HomeScreenState(true, ":q", null), Key(ConsoleKey.Enter), Keybindings);

        result.ShouldExit.Should().BeTrue();
    }

    [Fact]
    public void Reduce_reports_an_unknown_command_after_submission()
    {
        var result = reducer.Reduce(new HomeScreenState(true, ":unknown", null), Key(ConsoleKey.Enter), Keybindings);

        result.State.Should().Be(new HomeScreenState(false, string.Empty, "Unknown command: :unknown"));
        result.ShouldExit.Should().BeFalse();
    }

    [Fact]
    public void Reduce_cancels_command_mode_when_escape_is_pressed()
    {
        var result = reducer.Reduce(new HomeScreenState(true, ":unfinished", null), Key(ConsoleKey.Escape), Keybindings);

        result.State.Should().Be(HomeScreenState.Initial);
    }

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem1, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);
}
