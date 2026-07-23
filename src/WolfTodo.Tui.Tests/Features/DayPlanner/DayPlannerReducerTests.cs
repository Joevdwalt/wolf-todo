using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

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
        var returned = reducer.Reduce(tomorrow, Key('T'), Bindings, View(tomorrow)).State;

        tomorrow.SelectedDate.Should().Be(Today.AddDays(1));
        returned.SelectedDate.Should().Be(Today);
    }

    [Fact]
    public void Reduce_jumps_to_the_first_and_last_planner_slots()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today) with { SlotIndex = 12 };

        var first = reducer.Reduce(state, Key('g'), Bindings, View(state)).State;
        var last = reducer.Reduce(first, Key('G'), Bindings, View(first)).State;

        first.SlotIndex.Should().Be(0);
        last.SlotIndex.Should().Be(DayPlannerPresenter.SlotCount - 1);
    }

    [Fact]
    public void Reduce_opens_the_filtered_picker_from_an_empty_slot()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today);
        var view = View(state, Todo("Available"));

        var filtering = reducer.Reduce(state, Key('/'), Bindings, view).State;
        var typed = reducer.Reduce(filtering, Key('a'), Bindings, View(filtering, Todo("Available"))).State;

        filtering.Mode.Should().Be(PlannerMode.EditFilter);
        filtering.PickerIndex.Should().Be(0);
        typed.FilterDraft.Should().Be("a");
    }

    [Fact]
    public void Reduce_allows_filtering_from_an_occupied_slot_for_overlapping_work()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var scheduled = Todo("Scheduled") with { Schedule = new TodoSchedule(Today, new TimeOnly(6, 0)) };
        var state = PlannerState.CreateInitial(Today);

        var result = reducer.Reduce(state, Key('/'), Bindings, View(state, scheduled));

        result.State.Mode.Should().Be(PlannerMode.EditFilter);
        result.State.Error.Should().BeNull();
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
            Editor = selectedProject.State.Editor! with
            {
                Values = new TodoUpdate("New planned task", null, null, [], null, null)
            }
        };
        var saved = reducer.Reduce(
            withTitle,
            Key(ConsoleKey.S, control: true),
            Bindings,
            View(withTitle));

        opened.State.Editor.Should().NotBeNull();
        opened.State.Editor!.ScheduledDate.Should().Be("2026-07-15");
        opened.State.Editor.ScheduledTime.Should().Be("06:00");
        opened.State.Editor.ScheduleRequirement.Should().Be(TodoScheduleRequirement.DateAndTime);
        selectedProject.State.Editor!.ProjectPath.Should().Be("/todos/work.md");
        saved.Operation.Should().Be(PlannerOperation.Create);
        saved.Update!.Fields.Title.Should().Be("New planned task");
        saved.Update.Fields.Schedule.Should().Be(new TodoSchedule(Today, new TimeOnly(6, 0)));
        saved.State.Editor.Should().NotBeNull("the application clears it only after a successful write");
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

        edit.State.Editor.Should().NotBeNull();
        content.State.Editor.Should().NotBeNull();
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

    [Fact]
    public void Reduce_switches_focus_and_navigates_all_day_items_with_existing_pane_bindings()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var first = Todo("First") with { Schedule = new TodoSchedule(Today) };
        var second = Todo("Second") with { SourceLine = 2, Schedule = new TodoSchedule(Today) };
        var state = PlannerState.CreateInitial(Today);

        var allDay = reducer.Reduce(state, Key(ConsoleKey.Tab), Bindings, View(state, first, second)).State;
        var moved = reducer.Reduce(allDay, Key('j'), Bindings, View(allDay, first, second)).State;
        var timeline = reducer.Reduce(
            moved,
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, true, false, false),
            Bindings,
            View(moved, first, second)).State;

        allDay.Focus.Should().Be(PlannerFocus.AllDay);
        moved.AllDayIndex.Should().Be(1);
        timeline.Focus.Should().Be(PlannerFocus.Timeline);
    }

    [Fact]
    public void Reduce_assigns_picker_todos_to_an_all_day_destination()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today) with { Focus = PlannerFocus.AllDay };
        var view = View(state, Todo("Available"));

        var choosing = reducer.Reduce(state, Key(ConsoleKey.Enter), Bindings, view).State;
        var assigned = reducer.Reduce(choosing, Key(ConsoleKey.Enter), Bindings, View(choosing, Todo("Available")));

        choosing.Mode.Should().Be(PlannerMode.ChooseTodo);
        assigned.Operation.Should().Be(PlannerOperation.Schedule);
        assigned.ScheduleTarget.Should().Be(PlannerScheduleTarget.AllDay);
    }

    [Fact]
    public void Reduce_creates_all_day_tasks_with_a_required_date_and_no_required_time()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var state = PlannerState.CreateInitial(Today) with { Focus = PlannerFocus.AllDay };

        var opened = reducer.Reduce(state, Key('a'), Bindings, View(state));

        opened.State.Editor.Should().NotBeNull();
        opened.State.Editor!.ScheduledDate.Should().Be("2026-07-15");
        opened.State.Editor.ScheduledTime.Should().BeEmpty();
        opened.State.Editor.ScheduleRequirement.Should().Be(TodoScheduleRequirement.Date);
    }

    [Fact]
    public void Reduce_starts_move_for_an_all_day_todo_and_rejects_calendar_mutations()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var todo = Todo("All day") with { Schedule = new TodoSchedule(Today) };
        var state = PlannerState.CreateInitial(Today) with { Focus = PlannerFocus.AllDay };
        var todoView = View(state, todo);
        var calendarView = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([], []),
            state,
            new PlannerCalendarAgenda(
                [new PlannerCalendarAllDayItem("Holiday", PlannerCalendarItemKind.Event)],
                [],
                PlannerCalendarSyncState.Ready));

        var moving = reducer.Reduce(state, Key(ConsoleKey.Enter), Bindings, todoView);
        var readOnly = reducer.Reduce(state, Key('e'), Bindings, calendarView);

        moving.State.Mode.Should().Be(PlannerMode.MoveTodo);
        moving.State.MovingTodo.Should().Be(todoView.SelectedAllDayAssignment!.Identity);
        readOnly.State.Error.Should().Be("Calendar all-day items are read-only.");
    }

    [Fact]
    public void Reduce_exposes_edit_complete_and_unschedule_actions_for_all_day_todos()
    {
        var reducer = new DayPlannerReducer(() => Today);
        var todo = Todo("All day") with { Schedule = new TodoSchedule(Today) };
        var state = PlannerState.CreateInitial(Today) with { Focus = PlannerFocus.AllDay };
        var view = View(state, todo);

        var edit = reducer.Reduce(state, Key('e'), Bindings, view);
        var external = reducer.Reduce(state, Key(ConsoleKey.E, control: true), Bindings, view);
        var completed = reducer.Reduce(state, Key(ConsoleKey.Spacebar), Bindings, view);
        var unscheduled = reducer.Reduce(state, Key('u'), Bindings, view);

        edit.State.Editor.Should().NotBeNull();
        external.Operation.Should().Be(PlannerOperation.EditExternal);
        completed.Operation.Should().Be(PlannerOperation.ToggleCompleted);
        unscheduled.Operation.Should().Be(PlannerOperation.Unschedule);
        external.TodoIdentity.Should().Be(view.SelectedAllDayAssignment!.Identity);
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
