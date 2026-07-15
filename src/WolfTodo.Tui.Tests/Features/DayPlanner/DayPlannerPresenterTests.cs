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

    private static TodoItem Todo(string title) => new(
        1, false, null, title, null, [], null, null, string.Empty, [], []);
}
