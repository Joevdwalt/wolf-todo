using System.Collections.Immutable;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationActionCatalog
{
    public ImmutableArray<CommandPaletteItem> Create(
        bool browserActive,
        BrowserView? browser,
        PlannerView? planner,
        TuiKeyBindings bindings)
    {
        var browserReason = browserActive ? null : "Available in the Todos tab.";
        var plannerReason = browserActive ? "Available in the Day Planner tab." : null;
        var selectedReason = browserReason ?? (browser?.SelectedTodo is null ? "Select a todo first." : null);
        var browserCreateReason = browserReason ??
            (browser!.Projects.Any(project => project.Project is not null)
                ? null
                : "No valid projects are available.");
        var plannerCreateReason = plannerReason ??
            (planner!.Projects.Length == 0 ? "No valid projects are available." : null);
        var plannerAssignReason = plannerReason ??
            (planner!.SelectedSlot.Assignments.Length > 1
                ? "Resolve the conflicting timeslot first."
                : null);
        var plannerUnscheduleReason = plannerReason ?? planner!.SelectedSlot.Assignments.Length switch
        {
            0 => "No todo is assigned to this timeslot.",
            > 1 => "Resolve the conflicting timeslot first.",
            _ => null
        };
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
            Item(ApplicationActionId.BrowserEdit, "Todos", "Edit todo fields",
                "Edit title, tags, dates, priority, and reference", Shortest(bindings.EditTodo), selectedReason),
            Item(ApplicationActionId.BrowserEditContent, "Todos", "Edit notes and subtasks",
                "Open the structured content editor", Shortest(bindings.EditTodoContent), selectedReason),
            Item(ApplicationActionId.BrowserToggleCompleted, "Todos", "Toggle selected todo",
                "Change the selected checkbox", Shortest(bindings.ToggleTodo), selectedReason),
            Item(ApplicationActionId.PlannerPreviousDay, "Planner", "Previous day",
                "Move the planner back one day", Shortest(bindings.PlannerPreviousDay), plannerReason),
            Item(ApplicationActionId.PlannerNextDay, "Planner", "Next day",
                "Move the planner forward one day", Shortest(bindings.PlannerNextDay), plannerReason),
            Item(ApplicationActionId.PlannerToday, "Planner", "Go to today",
                "Return the planner to today", Shortest(bindings.PlannerToday), plannerReason),
            Item(ApplicationActionId.PlannerAssignOrMove, "Planner", "Assign or move todo",
                "Use the selected timeslot", Shortest(bindings.Open), plannerAssignReason),
            Item(ApplicationActionId.PlannerUnschedule, "Planner", "Unschedule todo",
                "Remove the selected assignment", Shortest(bindings.PlannerUnschedule), plannerUnscheduleReason),
            Item(ApplicationActionId.PlannerCreate, "Planner", "Create scheduled todo",
                "Create a todo in the selected slot", Shortest(bindings.CreateTodo), plannerCreateReason)
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
}
