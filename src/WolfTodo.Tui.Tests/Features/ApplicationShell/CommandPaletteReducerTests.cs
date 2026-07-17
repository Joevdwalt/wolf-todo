using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Core.Features.ProjectBrowser;

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

    [Fact]
    public void ActionCatalog_describes_the_next_details_visibility_action()
    {
        var visible = BrowserView(BrowserState.Initial);
        var hidden = BrowserView(BrowserState.Initial with { ShowDetails = false });
        var catalog = new ApplicationActionCatalog();

        var hide = catalog.Create(true, visible, null, Bindings)
            .Single(item => item.Action == ApplicationActionId.BrowserToggleDetails);
        var show = catalog.Create(true, hidden, null, Bindings)
            .Single(item => item.Action == ApplicationActionId.BrowserToggleDetails);

        hide.Label.Should().Be("Hide details");
        show.Label.Should().Be("Show details");
        hide.Binding.Should().Be("v");
        catalog.Create(true, visible, null, Bindings)
            .Single(item => item.Action == ApplicationActionId.BrowserJumpTop)
            .Binding.Should().Be("g");
        catalog.Create(true, visible, null, Bindings)
            .Single(item => item.Action == ApplicationActionId.BrowserJumpBottom)
            .Binding.Should().Be("G");
        catalog.Create(true, visible, null, Bindings)
            .Single(item => item.Action == ApplicationActionId.BrowserEditExternal)
            .Binding.Should().Be("Ctrl+E");
    }

    [Fact]
    public void ActionCatalog_enables_planner_editing_actions_for_an_occupied_slot()
    {
        var date = new DateOnly(2026, 7, 15);
        var todo = new TodoItem(
            1, false, null, "Scheduled", null, [], null, null, string.Empty, [], [])
        {
            Schedule = new TodoSchedule(date, new TimeOnly(6, 0))
        };
        var planner = new DayPlannerPresenter().CreateView(
            new ProjectCatalog(
                [new TodoProject("Work", "/todos/work.md", [todo])],
                []),
            PlannerState.CreateInitial(date));

        var items = new ApplicationActionCatalog().Create(false, null, planner, Bindings);

        items.Single(item => item.Action == ApplicationActionId.PlannerEdit).IsEnabled.Should().BeTrue();
        items.Single(item => item.Action == ApplicationActionId.PlannerEditContent).Binding.Should().Be("E");
        items.Single(item => item.Action == ApplicationActionId.PlannerEditExternal).Binding.Should().Be("Ctrl+E");
        items.Single(item => item.Action == ApplicationActionId.PlannerToggleCompleted).Binding.Should().Be("Spacebar");
        items.Single(item => item.Action == ApplicationActionId.PlannerToggleDetails).Label.Should().Be("Hide details");
    }

    private static BrowserView BrowserView(BrowserState state) => new(
        state,
        [new ProjectRow("All", 0, null, null, true)],
        [],
        null,
        "All",
        null,
        null,
        "No todos");

    private static System.Collections.Immutable.ImmutableArray<CommandPaletteItem> Items(
        params CommandPaletteItem[] items) => [.. items];

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
