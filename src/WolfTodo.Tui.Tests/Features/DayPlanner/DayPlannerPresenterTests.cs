using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class DayPlannerPresenterTests
{
    [Fact]
    public void CreateView_places_scheduled_todos_and_offers_only_open_unscheduled_todos()
    {
        var date = new DateOnly(2026, 7, 15);
        var scheduled = Todo("Scheduled") with { Schedule = new TodoSchedule(date, new TimeOnly(9, 30)) };
        var available = Todo("Available") with { SourceLine = 2 };
        var completed = Todo("Done") with { SourceLine = 3, IsCompleted = true };
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", [scheduled, available, completed])],
            []);

        var view = new DayPlannerPresenter().CreateView(
            catalog,
            PlannerState.CreateInitial(date) with { SlotIndex = 7 });

        view.Slots.Should().HaveCount(32);
        view.SelectedSlot.Time.Should().Be(new TimeOnly(9, 30));
        view.SelectedSlot.Assignments.Should().ContainSingle().Which.Todo.Title.Should().Be("Scheduled");
        view.PickerTodos.Should().ContainSingle().Which.Todo.Title.Should().Be("Available");
        view.OpenTodoCount.Should().Be(2);
        view.ProjectErrorCount.Should().Be(0);
    }

    [Fact]
    public void CreateView_exposes_conflicting_external_assignments()
    {
        var date = new DateOnly(2026, 7, 15);
        var schedule = new TodoSchedule(date, new TimeOnly(6, 0));
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", [
                Todo("One") with { Schedule = schedule },
                Todo("Two") with { SourceLine = 2, Schedule = schedule }
            ])],
            []);

        var view = new DayPlannerPresenter().CreateView(catalog, PlannerState.CreateInitial(date));

        view.SelectedSlot.Assignments.Should().HaveCount(2);
    }

    [Fact]
    public void CreateView_exposes_project_health_for_the_operational_header()
    {
        var date = new DateOnly(2026, 7, 15);
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", [Todo("Open")])],
            [new ProjectSourceError("Missing", "/todos/missing.md", "not found")]);

        var view = new DayPlannerPresenter().CreateView(catalog, PlannerState.CreateInitial(date));

        view.OpenTodoCount.Should().Be(1);
        view.ProjectErrorCount.Should().Be(1);
    }

    [Fact]
    public void CreateView_filters_picker_candidates_live_while_the_filter_is_edited()
    {
        var date = new DateOnly(2026, 7, 15);
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", [
                Todo("Prepare proposal") with { Tags = ["client"] },
                Todo("Buy groceries") with { SourceLine = 2 }
            ])],
            []);
        var state = PlannerState.CreateInitial(date) with
        {
            Mode = PlannerMode.EditFilter,
            FilterDraft = "client"
        };

        var view = new DayPlannerPresenter().CreateView(catalog, state);

        view.PickerTodos.Should().ContainSingle().Which.Todo.Title.Should().Be("Prepare proposal");
    }

    private static TodoItem Todo(string title) => new(
        1, false, null, title, null, [], null, null, string.Empty, [], []);
}
