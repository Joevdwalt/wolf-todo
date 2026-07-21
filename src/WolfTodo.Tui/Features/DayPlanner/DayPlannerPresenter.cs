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
                    .Where(assignment => Occupies(assignment.Todo, state.SelectedDate, time, configuration.DefaultDuration))
                    .ToImmutableArray();
                var slotEnd = time.AddMinutes(15);
                var meetings = agenda.Meetings
                    .Where(meeting => meeting.Start < slotEnd && meeting.End > time)
                    .OrderBy(meeting => meeting.Start)
                    .ThenBy(meeting => meeting.End)
                    .ThenBy(meeting => meeting.Title, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray();
                return new PlannerSlotView(time, items, index == slotIndex)
                {
                    Meetings = meetings,
                    PrimaryMeeting = meetings.FirstOrDefault(),
                    DurationPosition = items.Length == 1
                        ? DurationPosition(items[0].Todo, time, configuration.DefaultDuration)
                        : null,
                    MeetingDurationPosition = meetings.FirstOrDefault() is { } meeting
                        ? MeetingDurationPosition(meeting, time)
                        : null
                };
            })
            .ToImmutableArray();
        var selectedIdentity = slots[slotIndex].Assignments.Length == 1
            ? slots[slotIndex].Assignments[0].Identity
            : null;
        if (selectedIdentity is not null)
        {
            slots = slots
                .Select(slot => slot with
                {
                    IsActiveAssignment = slot.Assignments.Any(assignment => assignment.Identity == selectedIdentity)
                })
                .ToImmutableArray();
        }
        var selectedMeetingIdentity = slots[slotIndex].PrimaryMeeting?.Identity;
        if (selectedMeetingIdentity is not null)
        {
            slots = slots
                .Select(slot => slot with
                {
                    IsActiveMeeting = slot.PrimaryMeeting?.Identity == selectedMeetingIdentity
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

    private static bool Occupies(TodoItem todo, DateOnly date, TimeOnly time, TimeSpan defaultDuration)
    {
        var schedule = todo.Schedule;
        if (schedule?.Date != date || schedule.Time is null)
        {
            return false;
        }

        var end = schedule.Time.Value.Add(todo.Duration ?? defaultDuration);
        return time >= schedule.Time && time < end;
    }

    private static DurationBlockPosition? DurationPosition(
        TodoItem todo,
        TimeOnly time,
        TimeSpan defaultDuration)
    {
        var start = todo.Schedule?.Time;
        if (start is null)
        {
            return null;
        }

        var duration = todo.Duration ?? defaultDuration;
        if (duration <= TimeSpan.FromMinutes(15))
        {
            return null;
        }

        if (time == start)
        {
            return DurationBlockPosition.Start;
        }

        return time.AddMinutes(15) >= start.Value.Add(duration)
            ? DurationBlockPosition.End
            : DurationBlockPosition.Middle;
    }

    private static DurationBlockPosition? MeetingDurationPosition(PlannerCalendarMeeting meeting, TimeOnly time)
    {
        if (meeting.End - meeting.Start <= TimeSpan.FromMinutes(15))
        {
            return null;
        }

        if (meeting.Start >= time && meeting.Start < time.AddMinutes(15))
        {
            return DurationBlockPosition.Start;
        }

        return time.AddMinutes(15) >= meeting.End
            ? DurationBlockPosition.End
            : DurationBlockPosition.Middle;
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
