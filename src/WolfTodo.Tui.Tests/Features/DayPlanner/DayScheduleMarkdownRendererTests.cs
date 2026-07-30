using System.Collections.Immutable;
using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class DayScheduleMarkdownRendererTests
{
    [Fact]
    public void Render_writes_links_all_day_items_and_overlapping_titles()
    {
        var todo = new TodoItem(3, false, null, "Prepare proposal", null, [], null, null, "", [], [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 13), new TimeOnly(9, 15)),
            Duration = TimeSpan.FromMinutes(30)
        };
        var assignment = new PlannerAssignment(new TodoIdentity("/todo.md", 3), "Todos", "/todo.md", todo);
        var meeting = new PlannerCalendarMeeting("Management meeting", new TimeOnly(9, 0), new TimeOnly(10, 0));
        var slots = ImmutableArray.Create(
            Slot(new TimeOnly(9, 0), Meeting(meeting)),
            Slot(new TimeOnly(9, 15), Task(assignment), Meeting(meeting)),
            Slot(new TimeOnly(9, 30), Task(assignment), Meeting(meeting)),
            Slot(new TimeOnly(9, 45), Meeting(meeting)));
        var view = new PlannerView(PlannerState.CreateInitial(new DateOnly(2026, 7, 13)), slots, [], [])
        {
            CalendarAgenda = new PlannerCalendarAgenda(
                [new PlannerCalendarAllDayItem("Company holiday", PlannerCalendarItemKind.Event)], [], PlannerCalendarSyncState.Ready)
        };

        var result = new DayScheduleMarkdownRenderer().Render(
            view,
            new DayScheduleExportConfiguration("/notes", ["[[todos]]"]));

        result.Should().Contain("# 📅 Monday, 13 Jul 2026\n[[todos]]\n\n## All day\n- Company holiday")
            .And.Contain("**09:00 - 09:30** - Management meeting · Prepare proposal")
            .And.Contain("**09:30 - 10:00** - Management meeting · Prepare proposal")
            .And.Contain("**10:00 - 10:30** - ");
    }

    [Fact]
    public void Render_strikes_through_completed_todos_without_changing_calendar_items()
    {
        var todo = new TodoItem(3, true, null, "Complete proposal", null, [], null, null, "", [], [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 13), new TimeOnly(9, 0))
        };
        var assignment = new PlannerAssignment(new TodoIdentity("/todo.md", 3), "Todos", "/todo.md", todo);
        var meeting = new PlannerCalendarMeeting("Management meeting", new TimeOnly(9, 0), new TimeOnly(9, 30));
        var view = new PlannerView(
            PlannerState.CreateInitial(new DateOnly(2026, 7, 13)),
            [Slot(new TimeOnly(9, 0), Task(assignment, isCompleted: true), Meeting(meeting))], [], [])
        {
            CalendarAgenda = new PlannerCalendarAgenda(
                [
                    new PlannerCalendarAllDayItem("Completed all-day todo", PlannerCalendarItemKind.Todo, true),
                    new PlannerCalendarAllDayItem("Company holiday", PlannerCalendarItemKind.Event)
                ], [], PlannerCalendarSyncState.Ready)
        };

        var result = new DayScheduleMarkdownRenderer().Render(
            view,
            new DayScheduleExportConfiguration("/notes", []));

        result.Should().Contain("- ~~Completed all-day todo~~")
            .And.Contain("- Company holiday")
            .And.Contain("**09:00 - 09:30** - ~~Complete proposal~~ · Management meeting");
    }

    private static PlannerSlotView Slot(TimeOnly time, params PlannerTimelineItemView[] items) =>
        new(time, [], false) { Items = [.. items] };

    private static PlannerTimelineItemView Task(PlannerAssignment assignment, bool isCompleted = false) =>
        new(PlannerItemType.Task, "todo", assignment.Todo.Title, new TimeOnly(9, 15), new TimeOnly(9, 45),
            PlannerTimeShape.Duration, PlannerIntervalState.Start, isCompleted, false, assignment);

    private static PlannerTimelineItemView Meeting(PlannerCalendarMeeting meeting) =>
        new(PlannerItemType.Meeting, "meeting", meeting.Title, meeting.Start, meeting.End,
            PlannerTimeShape.Duration, PlannerIntervalState.Start, false, false, null, meeting);
}
