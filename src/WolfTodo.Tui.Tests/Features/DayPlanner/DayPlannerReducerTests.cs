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

    [Fact]
    public void Reduce_opens_the_shared_create_form_and_submits_a_scheduled_create()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today);
        var view = View(state);

        var opened = reducer.Reduce(state, Key('a'), Bindings, view);
        var selectedProject = reducer.Reduce(opened.State, Key(ConsoleKey.Enter), Bindings, View(opened.State));
        var withTitle = selectedProject.State with
        {
            Form = selectedProject.State.Form! with
            {
                Values = new TodoUpdate("New planned task", null, null, [], null, null)
            }
        };
        var saved = reducer.Reduce(
            withTitle,
            Key(ConsoleKey.S, control: true),
            Bindings,
            View(withTitle));

        opened.State.Form.Should().NotBeNull();
        selectedProject.State.Form!.ProjectPath.Should().Be("/todos/work.md");
        saved.Operation.Should().Be(PlannerOperation.Create);
        saved.Update!.Title.Should().Be("New planned task");
        saved.State.Form.Should().NotBeNull("the application clears it only after a successful write");
    }

    [Fact]
    public void Reduce_exposes_all_todo_editing_actions_for_an_occupied_slot()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var scheduled = Todo("Scheduled") with
        {
            Schedule = new TodoSchedule(Today, new TimeOnly(6, 0))
        };
        var state = PlannerState.CreateInitial(Today);
        var view = View(state, scheduled);

        var edit = reducer.Reduce(state, Key('e'), Bindings, view);
        var content = reducer.Reduce(state, Key('E'), Bindings, view);
        var external = reducer.Reduce(state, Key(ConsoleKey.E, control: true), Bindings, view);
        var completed = reducer.Reduce(state, Key(ConsoleKey.Spacebar), Bindings, view);

        edit.State.Form.Should().NotBeNull();
        content.State.ContentEditor.Should().NotBeNull();
        external.Operation.Should().Be(PlannerOperation.EditExternal);
        completed.Operation.Should().Be(PlannerOperation.ToggleCompleted);
        external.TodoIdentity.Should().Be(view.SelectedAssignment!.Identity);
        completed.TodoIdentity.Should().Be(view.SelectedAssignment.Identity);
    }

    [Fact]
    public void Reduce_toggles_planner_details_without_changing_the_selected_slot()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today) with { SlotIndex = 4 };

        var hidden = reducer.Reduce(state, Key('v'), Bindings, View(state));
        var restored = reducer.Reduce(hidden.State, Key('v'), Bindings, View(hidden.State));

        hidden.State.ShowDetails.Should().BeFalse();
        restored.State.ShowDetails.Should().BeTrue();
        restored.State.SlotIndex.Should().Be(4);
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

    private static ConsoleKeyInfo Key(ConsoleKey value, bool control = false) =>
        new('\0', value, false, false, control);
}
