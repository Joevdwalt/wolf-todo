using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayPlannerPresenter
{
    public const int SlotCount = 32;

    public PlannerView CreateView(ProjectCatalog catalog, PlannerState state)
    {
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
                var time = new TimeOnly(6, 0).AddMinutes(index * 30);
                var items = assignments
                    .Where(assignment => assignment.Todo.Schedule == new TodoSchedule(state.SelectedDate, time))
                    .ToImmutableArray();
                return new PlannerSlotView(time, items, index == slotIndex);
            })
            .ToImmutableArray();
        var projects = catalog.Projects
            .Select(project => new PlannerProjectOption(project.Title, project.Path))
            .ToImmutableArray();
        var createProjectIndex = Math.Clamp(
            state.CreateProjectIndex,
            0,
            Math.Max(0, projects.Length - 1));

        return new PlannerView(
            state with
            {
                SlotIndex = slotIndex,
                PickerIndex = pickerIndex,
                CreateProjectIndex = createProjectIndex
            },
            slots,
            picker,
            projects);
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
