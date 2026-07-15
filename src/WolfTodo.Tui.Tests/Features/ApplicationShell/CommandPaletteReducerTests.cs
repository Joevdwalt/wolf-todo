using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class CommandPaletteReducerTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");
    private readonly CommandPaletteReducer reducer = new();

    [Fact]
    public void Reduce_opens_searches_and_executes_an_enabled_action()
    {
        var items = Items(
            new CommandPaletteItem(
                ApplicationActionId.Exit, "Application", "Quit", "Exit", ":q", true, null),
            new CommandPaletteItem(
                ApplicationActionId.BrowserCreate, "Todos", "Create todo", "Add", "a", true, null));
        var closedView = new CommandPaletteView(CommandPaletteState.Closed, items);
        var opened = reducer.Reduce(CommandPaletteState.Closed, Key('?'), Bindings, closedView).State;
        var searching = reducer.Reduce(
            opened,
            Key('/'),
            Bindings,
            new CommandPaletteView(opened, items)).State;
        var typed = reducer.Reduce(
            searching,
            Key('r'),
            Bindings,
            new CommandPaletteView(searching, items)).State;
        var filtered = new CommandPalettePresenter().CreateView(typed, items);

        var executed = reducer.Reduce(typed, Key(ConsoleKey.Enter), Bindings, filtered);

        opened.IsOpen.Should().BeTrue();
        typed.Query.Should().Be("r");
        filtered.Items.Should().ContainSingle().Which.Action.Should().Be(ApplicationActionId.BrowserCreate);
        executed.Action.Should().Be(ApplicationActionId.BrowserCreate);
        executed.State.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Reduce_keeps_a_disabled_action_open_and_shows_its_reason()
    {
        var item = new CommandPaletteItem(
            ApplicationActionId.BrowserEdit,
            "Todos",
            "Edit todo",
            "Edit",
            "e",
            false,
            "Select a todo first.");
        var state = CommandPaletteState.Closed with { IsOpen = true };
        var view = new CommandPaletteView(state, Items(item));

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Bindings, view);

        result.Action.Should().BeNull();
        result.State.IsOpen.Should().BeTrue();
        result.State.Error.Should().Be("Select a todo first.");
    }

    [Fact]
    public void Reduce_treats_printable_vim_movement_as_search_text()
    {
        var state = CommandPaletteState.Closed with
        {
            IsOpen = true,
            IsSearching = true,
            SelectedIndex = 1
        };
        var items = Items(
            new CommandPaletteItem(ApplicationActionId.Exit, "Application", "Quit", "Exit", ":q", true, null),
            new CommandPaletteItem(
                ApplicationActionId.BrowserCreate, "Todos", "Create", "Add", "a", true, null));

        var result = reducer.Reduce(
            state,
            Key('k'),
            Bindings,
            new CommandPaletteView(state, items));

        result.State.Query.Should().Be("k");
        result.State.SelectedIndex.Should().Be(0);
    }

    private static System.Collections.Immutable.ImmutableArray<CommandPaletteItem> Items(
        params CommandPaletteItem[] items) => [.. items];

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
