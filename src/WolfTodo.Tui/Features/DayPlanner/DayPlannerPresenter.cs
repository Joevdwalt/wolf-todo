using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayPlannerPresenter
{
    public const int SlotCount = 64;

    public PlannerView CreateView(
        ProjectCatalog catalog,
        PlannerState state,
        PlannerCalendarAgenda? calendarAgenda = null,
        PlannerConfiguration? plannerConfiguration = null)
    {
        var configuration = plannerConfiguration ?? PlannerConfiguration.Default;
        var agenda = calendarAgenda ?? PlannerCalendarAgenda.Disabled;
        var assignments = catalog.Projects
            .SelectMany(project => Flatten(project.Todos)
                .Select(todo => new PlannerAssignment(
                    new TodoIdentity(project.Path, todo.SourceLine),
                    project.Title,
                    project.Path,
                    todo)))
            .ToArray();
        var slotIndex = Math.Clamp(state.SlotIndex, 0, SlotCount - 1);
        var filter = (state.Mode == PlannerMode.EditFilter ? state.FilterDraft : state.FilterText).Trim();
        var picker = assignments
            .Where(assignment => !assignment.Todo.IsCompleted && assignment.Todo.Schedule is null)
            .Where(assignment => Matches(assignment, filter))
            .OrderBy(assignment => assignment.ProjectTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(assignment => assignment.Todo.Title, NaturalStringComparer.Instance)
            .ToImmutableArray();
        var pickerIndex = Math.Clamp(state.PickerIndex, 0, Math.Max(0, picker.Length - 1));
        var slots = Enumerable.Range(0, SlotCount)
            .Select(index =>
            {
                var time = new TimeOnly(6, 0).AddMinutes(index * 15);
                var items = assignments
                    .Where(assignment => Occupies(assignment.Todo, state.SelectedDate, time))
                    .ToImmutableArray();
                var slotEnd = time.AddMinutes(15);
                var meetings = agenda.Meetings
                    .Where(meeting => meeting.Start < slotEnd && meeting.End > time)
                    .OrderBy(meeting => meeting.Start)
                    .ThenBy(meeting => meeting.End)
                    .ThenBy(meeting => meeting.Title, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray();
                var timelineItems = items
                    .OrderBy(assignment => assignment.ProjectTitle, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(assignment => assignment.Todo.Title, NaturalStringComparer.Instance)
                    .ThenBy(assignment => assignment.Todo.SourceLine)
                    .Select(assignment => TaskItem(assignment, time))
                    .Concat(meetings.Select(meeting => MeetingItem(meeting, time)))
                    .ToImmutableArray();
                return new PlannerSlotView(time, items, index == slotIndex)
                {
                    Items = timelineItems,
                    Meetings = meetings
                };
            })
            .ToImmutableArray();
        var selectedItemIdentity = slots[slotIndex].Items.FirstOrDefault()?.Identity;
        if (selectedItemIdentity is not null)
        {
            slots = slots
                .Select((slot, index) => slot with
                {
                    // The cursor belongs to the active slot, not every row of a
                    // duration item which happens to share the same identity.
                    // The active identity remains highlighted across its whole
                    // duration so the user can follow it through the timeline.
                    Items = [.. slot.Items.Select(item => item with
                    {
                        IsSelected = index == slotIndex && item.Identity == selectedItemIdentity,
                        IsActive = item.Identity == selectedItemIdentity
                    })],
                })
                .ToImmutableArray();
        }
        var projects = catalog.Projects
            .Select(project => new PlannerProjectOption(project.Title, project.Path))
            .ToImmutableArray();
        var allDayTodos = assignments
            .Where(assignment => assignment.Todo.Schedule?.Date == state.SelectedDate &&
                                 assignment.Todo.Schedule.Time is null)
            .Select(assignment => new PlannerCalendarAllDayItem(
                assignment.Todo.Title,
                PlannerCalendarItemKind.Todo,
                assignment.Todo.IsCompleted,
                assignment.Todo,
                assignment.ProjectTitle));
        return new PlannerView(
            state with
            {
                SlotIndex = slotIndex,
                PickerIndex = pickerIndex
            },
            slots,
            picker,
            projects)
        {
            OpenTodoCount = assignments.Count(assignment => !assignment.Todo.IsCompleted),
            ProjectErrorCount = catalog.Errors.Length,
            CalendarAgenda = agenda with { AllDayItems = [.. agenda.AllDayItems.Concat(allDayTodos)] }
        };
    }

    private static bool Occupies(TodoItem todo, DateOnly date, TimeOnly time)
    {
        var schedule = todo.Schedule;
        if (schedule?.Date != date || schedule.Time is null)
        {
            return false;
        }

        return todo.Duration is null
            ? time == schedule.Time
            : time >= schedule.Time && time < schedule.Time.Value.Add(todo.Duration.Value);
    }

    private static PlannerTimelineItemView TaskItem(PlannerAssignment assignment, TimeOnly time)
    {
        var todo = assignment.Todo;
        var start = todo.Schedule!.Time!.Value;
        var duration = todo.Duration;
        var end = duration is null ? start : start.Add(duration.Value);
        return new PlannerTimelineItemView(
            PlannerItemType.Task,
            $"task:{assignment.Identity.ProjectPath}:{assignment.Identity.SourceLine}",
            todo.Title,
            start,
            end,
            duration is null ? PlannerTimeShape.Instant : PlannerTimeShape.Duration,
            IntervalState(start, end, duration, time),
            todo.IsCompleted,
            false,
            assignment);
    }

    private static PlannerTimelineItemView MeetingItem(PlannerCalendarMeeting meeting, TimeOnly time)
    {
        var itemType = meeting.Attendees.Length > 0 ? PlannerItemType.Meeting : PlannerItemType.CalendarEvent;
        return new PlannerTimelineItemView(
            itemType,
            $"calendar:{meeting.Identity}",
            meeting.Title,
            meeting.Start,
            meeting.End,
            PlannerTimeShape.Duration,
            IntervalState(meeting.Start, meeting.End, meeting.End - meeting.Start, time),
            false,
            false,
            null,
            meeting);
    }

    private static PlannerIntervalState IntervalState(TimeOnly start, TimeOnly end, TimeSpan? duration, TimeOnly time)
    {
        if (duration is null)
        {
            return PlannerIntervalState.Instant;
        }

        if (duration <= TimeSpan.FromMinutes(15))
        {
            return PlannerIntervalState.StartAndEnd;
        }

        if (start >= time && start < time.AddMinutes(15))
        {
            return PlannerIntervalState.Start;
        }

        return time.AddMinutes(15) >= end ? PlannerIntervalState.End : PlannerIntervalState.Continue;
    }

    private static bool Matches(PlannerAssignment assignment, string filter) =>
        filter.Length == 0 ||
        Contains(assignment.Todo.Title, filter) ||
        Contains(assignment.Todo.ExternalReference, filter) ||
        Contains(assignment.ProjectTitle, filter) ||
        Contains(assignment.Todo.SectionPath, filter) ||
        assignment.Todo.Tags.Any(tag => Contains(tag, filter));

    private static bool Contains(string? value, string filter) =>
        value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;

    private static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var subtask in Flatten(todo.Subtasks))
            {
                yield return subtask;
            }
        }
    }
}
