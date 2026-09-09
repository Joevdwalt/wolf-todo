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
            .And.Contain("**09:00 - 09:30** - Management meeting · [Prepare proposal](../../../todo.md)")
            .And.Contain("**09:30 - 10:00** - Management meeting · [Prepare proposal](../../../todo.md)")
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
            .And.Contain("**09:00 - 09:30** - ~~[Complete proposal](../../../todo.md)~~ · Management meeting");
    }

    [Fact]
    public void Render_anchors_blocks_to_earliest_item_and_covers_final_item()
    {
        var early = new PlannerCalendarMeeting("Early meeting", new TimeOnly(8, 15), new TimeOnly(8, 45));
        var late = new PlannerCalendarMeeting("Late meeting", new TimeOnly(17, 45), new TimeOnly(18, 10));
        var view = new PlannerView(
            PlannerState.CreateInitial(new DateOnly(2026, 7, 13)),
            [Slot(early.Start, Meeting(early)), Slot(late.Start, Meeting(late))], [], []);

        var result = new DayScheduleMarkdownRenderer().Render(
            view,
            new DayScheduleExportConfiguration("/notes", []));

        result.Should().Contain("**08:15 - 08:45** - Early meeting")
            .And.Contain("**17:45 - 18:15** - Late meeting")
            .And.NotContain("**08:00 - 08:30**")
            .And.NotContain("**18:15 - 18:45**");
    }

    [Fact]
    public void Render_gives_a_late_instant_item_one_complete_block()
    {
        var item = new PlannerTimelineItemView(
            PlannerItemType.Task,
            "todo",
            "Late task",
            new TimeOnly(18, 0),
            new TimeOnly(18, 0),
            PlannerTimeShape.Instant,
            PlannerIntervalState.Instant,
            false,
            false);
        var view = new PlannerView(
            PlannerState.CreateInitial(new DateOnly(2026, 7, 13)),
            [Slot(item.Start, item)], [], []);

        var result = new DayScheduleMarkdownRenderer().Render(
            view,
            new DayScheduleExportConfiguration("/notes", []));

        result.Should().Contain("**18:00 - 18:30** - Late task")
            .And.NotContain("**18:30 - 19:00**");
    }

    [Fact]
    public void Render_keeps_default_range_when_there_are_no_timed_items()
    {
        var view = new PlannerView(PlannerState.CreateInitial(new DateOnly(2026, 7, 13)), [], [], []);

        var result = new DayScheduleMarkdownRenderer().Render(
            view,
            new DayScheduleExportConfiguration("/notes", []));

        result.Should().Contain("**09:00 - 09:30** - ")
            .And.Contain("**16:30 - 17:00** - ")
            .And.NotContain("**08:30 - 09:00**")
            .And.NotContain("**17:00 - 17:30**");
    }

    [Fact]
    public void Render_links_timed_and_all_day_todos_to_their_project_note()
    {
        var timedTodo = new TodoItem(3, false, null, "Prepare [proposal]", null, [], null, null, "", [], [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 13), new TimeOnly(9, 0))
        };
        var allDayTodo = new TodoItem(4, true, null, "Submit proposal", null, [], null, null, "", [], [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 13), null)
        };
        var projectPath = "/vault/projects/Client Work.md";
        var timedAssignment = new PlannerAssignment(
            new TodoIdentity(projectPath, 3), "Client", projectPath, timedTodo);
        var allDayAssignment = new PlannerAssignment(
            new TodoIdentity(projectPath, 4), "Client", projectPath, allDayTodo);
        var timedItem = new PlannerTimelineItemView(
            PlannerItemType.Task,
            "timed",
            timedTodo.Title,
            new TimeOnly(9, 0),
            new TimeOnly(9, 0),
            PlannerTimeShape.Instant,
            PlannerIntervalState.Instant,
            false,
            false,
            timedAssignment);
        var view = new PlannerView(
            PlannerState.CreateInitial(new DateOnly(2026, 7, 13)),
            [Slot(timedItem.Start, timedItem)], [], [])
        {
            CalendarAgenda = new PlannerCalendarAgenda(
                [new PlannerCalendarAllDayItem(allDayTodo.Title, PlannerCalendarItemKind.Todo, true)
                {
                    Assignment = allDayAssignment
                }], [], PlannerCalendarSyncState.Ready)
        };

        var result = new DayScheduleMarkdownRenderer().Render(
            view,
            new DayScheduleExportConfiguration("/vault/notes", []));

        result.Should().Contain("[Prepare \\[proposal\\]](../../../projects/Client%20Work.md)")
            .And.Contain("- ~~[Submit proposal](../../../projects/Client%20Work.md)~~");
    }

    [Fact]
    public void Render_excludes_temporary_pomodoro_blocks()
    {
        var pomodoro = new PlannerTimelineItemView(
            PlannerItemType.Pomodoro,
            "pomodoro",
            "Deep work",
            new TimeOnly(9, 0),
            new TimeOnly(9, 30),
            PlannerTimeShape.Duration,
            PlannerIntervalState.Start,
            false,
            false);
        var view = new PlannerView(
            PlannerState.CreateInitial(new DateOnly(2026, 7, 13)),
            [Slot(new TimeOnly(9, 0), pomodoro)],
            [],
            []);

        var result = new DayScheduleMarkdownRenderer().Render(
            view,
            new DayScheduleExportConfiguration("/notes", []));

        result.Should().NotContain("Deep work");
    }

    private static PlannerSlotView Slot(TimeOnly time, params PlannerTimelineItemView[] items) =>
        new(time, [], false) { Items = [.. items] };

    private static PlannerTimelineItemView Task(PlannerAssignment assignment, bool isCompleted = false) =>
        new(PlannerItemType.Task, "todo", assignment.Todo.Title, new TimeOnly(9, 15), new TimeOnly(9, 45),
            PlannerTimeShape.Duration, PlannerIntervalState.Start, isCompleted, false, assignment);

    private static PlannerTimelineItemView Meeting(PlannerCalendarMeeting meeting) =>
        new(PlannerItemType.Meeting, meeting.Identity, meeting.Title, meeting.Start, meeting.End,
            PlannerTimeShape.Duration, PlannerIntervalState.Start, false, false, null, meeting);
}
