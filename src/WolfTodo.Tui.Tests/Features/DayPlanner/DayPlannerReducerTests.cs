using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class DayPlannerReducerTests
{
    private static readonly DateOnly Today = new(2026, 7, 15);
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");

    [Fact]
    public void Reduce_moves_between_dates_and_returns_to_today()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today);
        var view = View(state);

        var tomorrow = reducer.Reduce(state, Key(']'), Bindings, view).State;
        var returned = reducer.Reduce(tomorrow, Key('g'), Bindings, View(tomorrow)).State;

        tomorrow.SelectedDate.Should().Be(Today.AddDays(1));
        returned.SelectedDate.Should().Be(Today);
    }

    [Fact]
    public void Reduce_opens_the_picker_and_assigns_its_selected_todo()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today);
        var view = View(state, Todo("Available"));

        var choosing = reducer.Reduce(state, Key(ConsoleKey.Enter), Bindings, view).State;
        var choosingView = View(choosing, Todo("Available"));
        var assigned = reducer.Reduce(choosing, Key(ConsoleKey.Enter), Bindings, choosingView);

        choosing.Mode.Should().Be(PlannerMode.ChooseTodo);
        assigned.Operation.Should().Be(PlannerOperation.Schedule);
        assigned.TodoIdentity.Should().NotBeNull();
    }

    private static PlannerView View(PlannerState state, params TodoItem[] todos)
    {
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", [.. todos])],
            []);
        return new DayPlannerPresenter().CreateView(catalog, state);
    }

    private static TodoItem Todo(string title) => new(
        1, false, null, title, null, [], null, null, string.Empty, [], []);

    private static ConsoleKeyInfo Key(char value) => new(value, ConsoleKey.Oem1, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey value) => new('\0', value, false, false, false);
}
