using System.Collections.Immutable;
using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.Configuration;

public sealed class TuiKeyBindingsTests
{
    [Fact]
    public void CreateDefaults_supports_current_vim_and_tab_navigation_keys()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q");

        bindings.MatchesMoveUp(Key(ConsoleKey.UpArrow)).Should().BeTrue();
        bindings.MatchesMoveUp(Key('k')).Should().BeTrue();
        bindings.MatchesMoveDown(Key(ConsoleKey.DownArrow)).Should().BeTrue();
        bindings.MatchesMoveDown(Key('j')).Should().BeTrue();
        bindings.MatchesOpen(Key(ConsoleKey.Enter)).Should().BeTrue();
        bindings.MatchesOpen(Key('l')).Should().BeTrue();
        bindings.MatchesBack(Key(ConsoleKey.Escape)).Should().BeTrue();
        bindings.MatchesBack(Key('h')).Should().BeTrue();
        bindings.MatchesSortMode(Key('t')).Should().BeTrue();
        bindings.MatchesTabNext(Key('L')).Should().BeTrue();
        bindings.MatchesTabPrevious(Key('H')).Should().BeTrue();
        bindings.MatchesPlannerPreviousDay(Key('[')).Should().BeTrue();
        bindings.MatchesPlannerNextDay(Key(']')).Should().BeTrue();
        bindings.MatchesPlannerToday(Key('g')).Should().BeTrue();
        bindings.MatchesPlannerUnschedule(Key('u')).Should().BeTrue();
        bindings.MatchesCreateTodo(Key('a')).Should().BeTrue();
        bindings.MatchesEditTodo(Key('e')).Should().BeTrue();
        bindings.MatchesEditTodoContent(Key('E')).Should().BeTrue();
        bindings.MatchesToggleTodo(Key(ConsoleKey.Spacebar)).Should().BeTrue();
        bindings.MatchesRemoveContent(Key('d')).Should().BeTrue();
        bindings.MatchesCommandPalette(Key('?')).Should().BeTrue();
        bindings.MatchesSaveForm(Key(ConsoleKey.S, control: true)).Should().BeTrue();
        bindings.HelpCommand.Should().Be(":help");
    }

    [Fact]
    public void ShortestDisplayName_prefers_the_first_shortest_binding()
    {
        var gestures = new[] { "Ctrl+K", "k", "j" }.Select(KeyGesture.Parse).ToImmutableArray();

        var result = TuiKeyBindings.ShortestDisplayName(gestures);

        result.Should().Be("k");
    }

    private static ConsoleKeyInfo Key(
        ConsoleKey key,
        bool shift = false,
        bool control = false) => new('\0', key, shift, false, control);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.NoName, false, false, false);
}
