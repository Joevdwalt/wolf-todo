using System.Collections.Immutable;
using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.Configuration;

public sealed class BrowserKeyBindingsTests
{
    [Fact]
    public void CreateDefaults_supports_current_and_vim_navigation_keys()
    {
        var bindings = BrowserKeyBindings.CreateDefaults(":q");

        bindings.MatchesMoveUp(Key(ConsoleKey.UpArrow)).Should().BeTrue();
        bindings.MatchesMoveUp(Key('k')).Should().BeTrue();
        bindings.MatchesMoveDown(Key(ConsoleKey.DownArrow)).Should().BeTrue();
        bindings.MatchesMoveDown(Key('j')).Should().BeTrue();
        bindings.MatchesOpen(Key(ConsoleKey.Enter)).Should().BeTrue();
        bindings.MatchesOpen(Key('l')).Should().BeTrue();
        bindings.MatchesBack(Key(ConsoleKey.Escape)).Should().BeTrue();
        bindings.MatchesBack(Key('h')).Should().BeTrue();
    }

    [Fact]
    public void ShortestDisplayName_prefers_the_first_shortest_binding()
    {
        var gestures = new[] { "Ctrl+K", "k", "j" }.Select(KeyGesture.Parse).ToImmutableArray();

        var result = BrowserKeyBindings.ShortestDisplayName(gestures);

        result.Should().Be("k");
    }

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.NoName, false, false, false);
}
