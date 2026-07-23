using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationActionCatalog(Func<DateOnly>? todayProvider = null)
{
    private readonly Func<DateOnly> todayProvider = todayProvider ??
        (() => DateOnly.FromDateTime(DateTime.Today));

    public ImmutableArray<CommandPaletteItem> Create(
        bool browserActive,
        BrowserView? browser,
        PlannerView? planner,
        TuiKeyBindings bindings)
    {
        var browserReason = browserActive ? null : "Available in the Todos tab.";
        var plannerReason = browserActive ? "Available in the Day Planner tab." : null;
        var selectedReason = browserReason ?? (browser?.SelectedTodo is null ? "Select a todo first." : null);
        var selectedProject = browser?.Projects.FirstOrDefault(project => project.IsSelected)?.Project;
        var rollReason = browserReason ?? (selectedProject is null
            ? "Select a project first."
            : Flatten(selectedProject.Todos).Any(todo =>
                !todo.IsCompleted && todo.Schedule?.Date < todayProvider())
                ? null
                : "The selected project has no incomplete overdue tasks.");
        var browserCreateReason = browserReason ??
            (browser!.Projects.Any(project => project.Project is not null)
                ? null
                : "No valid projects are available.");
        var readOnlyAllDay = planner?.State.Focus == PlannerFocus.AllDay &&
                             planner.SelectedAllDayItem is not null &&
                             planner.SelectedAllDayAssignment is null;
        var plannerCreateReason = plannerReason ?? (planner!.Projects.Length == 0
            ? "No valid projects are available."
            : null);
        var plannerSelectedReason = plannerReason ?? (readOnlyAllDay
            ? "Calendar all-day items are read-only."
            : planner!.SelectedFocusedAssignment is not null
                ? null
                : planner.State.Focus == PlannerFocus.AllDay
                    ? "No todo is selected in All Day."
                    : planner.SelectedSlot.Assignments.Length > 1
                        ? "Resolve the conflicting timeslot first."
                        : "No todo is assigned to this timeslot.");
        var plannerAssignReason = plannerReason ?? (readOnlyAllDay
            ? "Calendar all-day items are read-only."
            : planner!.State.Focus == PlannerFocus.Timeline && planner.SelectedSlot.Assignments.Length > 1
                ? "Resolve the conflicting timeslot first."
                : null);
        var plannerUnscheduleReason = plannerSelectedReason;
        return
        [
            Item(ApplicationActionId.Exit, "Application", "Quit", "Exit Wolf Todo",
                bindings.QuitCommand),
            Item(ApplicationActionId.ToggleCompleted, "Application", "Toggle completed",
                "Show or hide completed todos", bindings.ToggleCompletedCommand),
            Item(ApplicationActionId.NextTab, "Application", "Next tab", "Select the next application tab",
                Shortest(bindings.TabNext)),
            Item(ApplicationActionId.PreviousTab, "Application", "Previous tab",
                "Select the previous application tab", Shortest(bindings.TabPrevious)),
            Item(ApplicationActionId.BrowserFilter, "Todos", "Filter todos", "Edit the todo filter",
                Shortest(bindings.FilterMode), browserReason),
            Item(ApplicationActionId.BrowserSort, "Todos", "Sort todos", "Choose a presentation order",
                Shortest(bindings.SortMode), browserReason),
            Item(ApplicationActionId.BrowserCreate, "Todos", "Create todo", "Add a todo to a project",
                Shortest(bindings.CreateTodo), browserCreateReason),
            Item(ApplicationActionId.BrowserEdit, "Todos", "Edit todo",
                "Edit fields, notes, and subtasks in one dialog", Shortest(bindings.EditTodo), selectedReason),
            Item(ApplicationActionId.BrowserEditExternal, "Todos", "Edit in $EDITOR",
                "Open the Markdown source at the selected todo", Shortest(bindings.EditTodoExternal), selectedReason),
            Item(ApplicationActionId.BrowserToggleCompleted, "Todos", "Toggle selected todo",
                "Change the selected checkbox", Shortest(bindings.ToggleTodo), selectedReason),
            Item(ApplicationActionId.BrowserRollProjectToday, "Todos", "Roll project to today",
                "Move incomplete overdue tasks in the selected project to today",
                Shortest(bindings.RollProjectToday), rollReason),
            Item(ApplicationActionId.BrowserToggleDetails, "Todos",
                browser?.State.ShowDetails == false ? "Show details" : "Hide details",
                "Show or hide the selected todo preview", Shortest(bindings.ToggleDetails), browserReason),
            Item(ApplicationActionId.BrowserJumpTop, "Todos", "Jump to top",
                "Select the first item in the focused list", Shortest(bindings.JumpTop), browserReason),
            Item(ApplicationActionId.BrowserJumpBottom, "Todos", "Jump to bottom",
                "Select the last item in the focused list", Shortest(bindings.JumpBottom), browserReason),
            Item(ApplicationActionId.PlannerPreviousDay, "Planner", "Previous day",
                "Move the planner back one day", Shortest(bindings.PlannerPreviousDay), plannerReason),
            Item(ApplicationActionId.PlannerNextDay, "Planner", "Next day",
                "Move the planner forward one day", Shortest(bindings.PlannerNextDay), plannerReason),
            Item(ApplicationActionId.PlannerToday, "Planner", "Go to today",
                "Return the planner to today", Shortest(bindings.PlannerToday), plannerReason),
            Item(ApplicationActionId.PlannerRefreshCalendar, "Planner", "Refresh calendar",
                "Connect to or refresh the primary Google Calendar", Shortest(bindings.PlannerRefreshCalendar),
                plannerReason),
            Item(ApplicationActionId.PlannerAssignOrMove, "Planner", "Assign or move todo",
                "Use the selected planner destination", Shortest(bindings.Open), plannerAssignReason),
            Item(ApplicationActionId.PlannerUnschedule, "Planner", "Unschedule todo",
                "Remove the selected assignment", Shortest(bindings.PlannerUnschedule), plannerUnscheduleReason),
            Item(ApplicationActionId.PlannerCreate, "Planner", "Create scheduled todo",
                "Create a todo in the selected planner destination", Shortest(bindings.CreateTodo), plannerCreateReason),
            Item(ApplicationActionId.PlannerEdit, "Planner", "Edit todo",
                "Edit fields, notes, and subtasks in one dialog", Shortest(bindings.EditTodo), plannerSelectedReason),
            Item(ApplicationActionId.PlannerEditExternal, "Planner", "Edit in $EDITOR",
                "Open the Markdown source at the selected todo", Shortest(bindings.EditTodoExternal),
                plannerSelectedReason),
            Item(ApplicationActionId.PlannerToggleCompleted, "Planner", "Toggle selected todo",
                "Change the selected checkbox", Shortest(bindings.ToggleTodo), plannerSelectedReason),
            Item(ApplicationActionId.PlannerToggleDetails, "Planner",
                planner?.State.ShowDetails == false ? "Show details" : "Hide details",
                "Show or hide the selected todo preview", Shortest(bindings.ToggleDetails), plannerReason)
        ];
    }

    private static CommandPaletteItem Item(
        ApplicationActionId action,
        string group,
        string label,
        string description,
        string binding,
        string? disabledReason = null) =>
        new(action, group, label, description, binding, disabledReason is null, disabledReason);

    private static string Shortest(System.Collections.Immutable.ImmutableArray<KeyGesture> gestures) =>
        TuiKeyBindings.ShortestDisplayName(gestures);

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
