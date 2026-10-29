using WolfTodo.Tui.Features.TaskFocus;
using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.Commands;

public sealed class ApplicationActionCatalog(Func<DateOnly>? todayProvider = null)
{
    private readonly Func<DateOnly> todayProvider = todayProvider ??
        (() => DateOnly.FromDateTime(DateTime.Today));

    public ImmutableArray<CommandPaletteItem> Create(
        bool browserActive,
        BrowserView? browser,
        PlannerView? planner,
        TuiKeyBindings bindings,
        bool plannerExportEnabled = false,
        bool timerEnabled = false,
        bool timerRunning = false,
        FocusedTaskView? focusedTask = null)
    {
        if (focusedTask is not null)
        {
            return CreateFocusItems(bindings, timerEnabled, timerRunning);
        }

        var availability = GetAvailability(browserActive, browser, planner, plannerExportEnabled, timerEnabled, timerRunning);
        return
        [
            .. CreateApplicationItems(browserActive, planner, bindings, timerRunning, availability),
            .. CreateBrowserItems(browser, bindings, availability),
            .. CreatePlannerItems(planner, bindings, availability)
        ];
    }

    private static ImmutableArray<CommandPaletteItem> CreateFocusItems(
        TuiKeyBindings bindings, bool timerEnabled, bool timerRunning)
    {
        var focusTimerReason = timerRunning
            ? null
            : timerEnabled ? null : "Configure [timer] to enable task timing.";
        var focusPomodoroReason = !timerEnabled
            ? "Configure [timer] to enable Pomodoro timing."
            : timerRunning ? "Stop the active timer first." : null;
        return
        [
            Item(ApplicationActionId.Exit, "Application", "Quit", "Exit Wolf Todo",
                bindings.QuitCommand),
            Item(ApplicationActionId.OpenConfiguration, "Application", "Edit configuration",
                "Open config.toml in $EDITOR", ApplicationCommandCatalog.Configuration),
            Item(ApplicationActionId.GenerateTaskLink, "Application", "Generate task link",
                "Display a code for the highlighted Markdown location", ApplicationCommandCatalog.TaskLink),
            Item(ApplicationActionId.OpenTaskLink, "Application", "Open task link",
                "Enter a task code to open in Todos", ApplicationCommandCatalog.OpenTask),
            Item(ApplicationActionId.ExitTaskFocus, "Focus", "Exit focus",
                "Return to the previous view", Shortest(bindings.Back)),
            Item(ApplicationActionId.FocusEdit, "Focus", "Edit highlighted task",
                "Edit fields, notes, schedule, and subtasks", Shortest(bindings.EditTodoContent)),
            Item(ApplicationActionId.FocusEditExternal, "Focus", "Edit in $EDITOR",
                "Open the highlighted task in its Markdown source", Shortest(bindings.EditTodoExternal)),
            Item(ApplicationActionId.FocusToggleCompleted, "Focus", "Toggle highlighted task",
                "Change the highlighted checkbox", Shortest(bindings.ToggleTodo)),
            Item(ApplicationActionId.ToggleTimer, "Focus", timerRunning ? "Stop timer" : "Start timer",
                "Start, stop, or switch timing for the highlighted task",
                Shortest(bindings.ToggleTimer), focusTimerReason),
            Item(ApplicationActionId.StartPomodoro, "Focus", "Start Pomodoro",
                "Start a countdown for the highlighted task",
                Shortest(bindings.StartPomodoro), focusPomodoroReason)
        ];
    }

    private Availability GetAvailability(
        bool browserActive, BrowserView? browser, PlannerView? planner,
        bool plannerExportEnabled, bool timerEnabled, bool timerRunning)
    {
        var browserReasons = GetBrowserAvailability(browserActive, browser);
        var plannerReasons = GetPlannerAvailability(browserActive, planner, plannerExportEnabled);
        var timerReason = timerRunning ? null : !timerEnabled
            ? "Configure [timer] to enable task timing."
            : browserActive ? browserReasons.SelectedReason : plannerReasons.SelectedReason;
        var pomodoroReason = !timerEnabled ? "Configure [timer] to enable Pomodoro timing."
            : timerRunning ? "Stop the active timer first." : null;
        return new Availability(browserReasons, plannerReasons, timerReason, pomodoroReason);
    }

    private BrowserAvailability GetBrowserAvailability(bool browserActive, BrowserView? browser)
    {
        var reason = browserActive ? null : "Available in the Todos tab.";
        var selected = reason ?? (browser?.SelectedTodo is null ? "Select a todo first." : null);
        var marked = reason ?? (browser?.State.MarkedTodos.Count > 0 ? null : "Mark at least one todo first.");
        var project = browser?.Projects.FirstOrDefault(item => item.IsSelected)?.Project;
        var roll = reason ?? (project is null ? "Select a project first."
            : TodoTree.Enumerate(project.Todos).Any(todo => !todo.IsCompleted && todo.Schedule?.Date < todayProvider())
                ? null : "The selected project has no incomplete overdue tasks.");
        var create = reason ?? (browser!.Projects.Any(item => item.Project is not null)
            ? null : "No valid projects are available.");
        return new BrowserAvailability(reason, selected, marked, roll, create);
    }

    private static PlannerAvailability GetPlannerAvailability(
        bool browserActive, PlannerView? planner, bool plannerExportEnabled)
    {
        var reason = browserActive ? "Available in the Day Planner tab." : null;
        var readOnly = planner?.State.Focus == PlannerFocus.AllDay &&
                       planner.SelectedAllDayItem is not null && planner.SelectedAllDayAssignment is null;
        var create = reason ?? (planner!.Projects.Length == 0 ? "No valid projects are available." : null);
        var selected = reason ?? (readOnly ? "Calendar all-day items are read-only."
            : planner!.SelectedFocusedAssignment is not null ? null
            : planner.State.Focus == PlannerFocus.AllDay ? "No todo is selected in All Day."
            : planner.SelectedSlot.Assignments.Length > 1 ? "Resolve the conflicting timeslot first."
            : "No todo is assigned to this timeslot.");
        var assign = reason ?? (readOnly ? "Calendar all-day items are read-only."
            : planner!.State.Focus == PlannerFocus.Timeline && planner.SelectedSlot.Assignments.Length > 1
                ? "Resolve the conflicting timeslot first." : null);
        var export = reason ?? (plannerExportEnabled ? null : "Configure [planner.export] to enable day schedule export.");
        var day = reason ?? (planner!.State.ViewMode == PlannerViewMode.MultiDay
            ? "Switch to single-day view first." : null);
        return new PlannerAvailability(reason, create, selected, assign, export, day);
    }

    private static ImmutableArray<CommandPaletteItem> CreateApplicationItems(
        bool browserActive, PlannerView? planner, TuiKeyBindings bindings, bool timerRunning, Availability availability) =>
    [
            Item(ApplicationActionId.Exit, "Application", "Quit", "Exit Wolf Todo",
                bindings.QuitCommand),
            Item(ApplicationActionId.OpenConfiguration, "Application", "Edit configuration",
                "Open config.toml in $EDITOR", ApplicationCommandCatalog.Configuration),
            Item(ApplicationActionId.GenerateTaskLink, "Application", "Generate task link",
                "Display a code for the selected Markdown location", ApplicationCommandCatalog.TaskLink,
                browserActive ? availability.Browser.SelectedReason : planner?.SelectedFocusedAssignment is null
                    ? "Select a Markdown task first." : null),
            Item(ApplicationActionId.OpenTaskLink, "Application", "Open task link",
                "Enter a task code to open in Todos", ApplicationCommandCatalog.OpenTask),
            Item(ApplicationActionId.ToggleCompleted, "Application", "Toggle completed",
                "Show or hide completed todos", bindings.ToggleCompletedCommand),
            Item(ApplicationActionId.ToggleTimer, "Application", timerRunning ? "Stop timer" : "Start timer",
                "Start or stop timing the selected todo", Shortest(bindings.ToggleTimer), availability.TimerReason),
            Item(ApplicationActionId.StartPomodoro, "Application", "Start Pomodoro",
                "Start for the selected todo, or untracked when none is selected",
                Shortest(bindings.StartPomodoro), availability.PomodoroReason),
            Item(ApplicationActionId.StartUntrackedPomodoro, "Application", "Start untracked Pomodoro",
                "Start a Pomodoro without a todo or time-log entry",
                Shortest(bindings.StartUntrackedPomodoro), availability.PomodoroReason),
            Item(ApplicationActionId.FocusSelectedTask, "Application", "Focus selected task",
                "Hide other views and work on the selected task", Shortest(bindings.FocusTask),
                browserActive ? availability.Browser.SelectedReason : availability.Planner.SelectedReason),
            Item(ApplicationActionId.NextTab, "Application", "Next tab", "Select the next application tab",
                Shortest(bindings.TabNext)),
            Item(ApplicationActionId.PreviousTab, "Application", "Previous tab",
                "Select the previous application tab", Shortest(bindings.TabPrevious))
    ];

    private static ImmutableArray<CommandPaletteItem> CreateBrowserItems(
        BrowserView? browser, TuiKeyBindings bindings, Availability availability) =>
    [
            Item(ApplicationActionId.BrowserFilter, "Todos", "Filter todos", "Edit the todo filter",
                Shortest(bindings.FilterMode), availability.Browser.Reason),
            Item(ApplicationActionId.BrowserSort, "Todos", "Sort todos", "Choose a presentation order",
                Shortest(bindings.SortMode), availability.Browser.Reason),
            Item(ApplicationActionId.BrowserCreate, "Todos", "Create todo", "Add a todo to a project",
                Shortest(bindings.CreateTodo), availability.Browser.CreateReason),
            Item(ApplicationActionId.BrowserEdit, "Todos", "Edit todo",
                "Edit fields, notes, and subtasks in one dialog", Shortest(bindings.EditTodo), availability.Browser.SelectedReason),
            Item(ApplicationActionId.BrowserEditExternal, "Todos", "Edit in $EDITOR",
                "Open the Markdown source at the selected todo", Shortest(bindings.EditTodoExternal), availability.Browser.SelectedReason),
            Item(ApplicationActionId.BrowserToggleCompleted, "Todos", "Toggle selected todo",
                "Change the selected checkbox", Shortest(bindings.ToggleTodo), availability.Browser.SelectedReason),
            Item(ApplicationActionId.BrowserToggleSelection, "Todos", "Mark or unmark todo",
                "Toggle the cursor task in the bulk selection",
                Shortest(bindings.ToggleTodoSelection), availability.Browser.SelectedReason),
            Item(ApplicationActionId.BrowserBulkEdit, "Todos", "Bulk edit marked tasks",
                "Update schedule, tags, priority, or completion",
                Shortest(bindings.BulkEditTodos), availability.Browser.MarkedReason),
            Item(ApplicationActionId.BrowserClearSelection, "Todos", "Clear task marks",
                "Remove every task from the bulk selection",
                Shortest(bindings.ClearTodoSelection), availability.Browser.MarkedReason),
            Item(ApplicationActionId.BrowserRollProjectToday, "Todos", "Roll project to today",
                "Move incomplete overdue tasks in the selected project to today",
                Shortest(bindings.RollProjectToday), availability.Browser.RollReason),
            Item(ApplicationActionId.BrowserToggleDetails, "Todos",
                browser?.State.ShowDetails == false ? "Show details" : "Hide details",
                "Show or hide the selected todo preview", Shortest(bindings.ToggleDetails), availability.Browser.Reason),
            Item(ApplicationActionId.BrowserToggleSubtasks, "Todos", "Collapse or expand subtasks",
                "Toggle the selected task branch", Shortest(bindings.ToggleSubtasks),
                browser?.State.Focus == BrowserFocus.Todos && browser.SelectedTodoIdentity is { } selected &&
                browser.ExpandableTodos.Contains(selected) ? availability.Browser.Reason : "Select a task with subtasks"),
            Item(ApplicationActionId.BrowserToggleAllSubtasks, "Todos", "Collapse or expand all subtasks",
                "Toggle branches in the selected sidebar view", Shortest(bindings.ToggleAllSubtasks),
                browser?.ExpandableTodos.IsEmpty == false ? availability.Browser.Reason : "No subtasks in this view"),
            Item(ApplicationActionId.BrowserJumpTop, "Todos", "Jump to top",
                "Select the first item in the focused list", Shortest(bindings.JumpTop), availability.Browser.Reason),
            Item(ApplicationActionId.BrowserJumpBottom, "Todos", "Jump to bottom",
                "Select the last item in the focused list", Shortest(bindings.JumpBottom), availability.Browser.Reason)
    ];

    private static ImmutableArray<CommandPaletteItem> CreatePlannerItems(
        PlannerView? planner, TuiKeyBindings bindings, Availability availability) =>
    [
            Item(ApplicationActionId.PlannerPreviousDay, "Planner", "Previous day",
                "Move the planner back one day", Shortest(bindings.PlannerPreviousDay), availability.Planner.DayReason),
            Item(ApplicationActionId.PlannerNextDay, "Planner", "Next day",
                "Move the planner forward one day", Shortest(bindings.PlannerNextDay), availability.Planner.DayReason),
            Item(ApplicationActionId.PlannerToday, "Planner", "Jump to now",
                "Select today's current quarter-hour", Shortest(bindings.PlannerToday), availability.Planner.Reason),
            Item(ApplicationActionId.PlannerToggleView, "Planner",
                planner?.State.ViewMode == PlannerViewMode.MultiDay ? "Show single-day view" : "Show multiday view",
                "Switch between the day and multiday planner", Shortest(bindings.PlannerToggleView), availability.Planner.Reason),
            Item(ApplicationActionId.PlannerIncreaseRange, "Planner", "Show more dates",
                "Increase the multiday range by one date", Shortest(bindings.PlannerIncreaseRange),
                availability.Planner.Reason ?? (planner!.State.ViewMode == PlannerViewMode.MultiDay ? null : "Switch to multiday view first.")),
            Item(ApplicationActionId.PlannerDecreaseRange, "Planner", "Show fewer dates",
                "Decrease the multiday range by one date", Shortest(bindings.PlannerDecreaseRange),
                availability.Planner.Reason ?? (planner!.State.ViewMode == PlannerViewMode.MultiDay ? null : "Switch to multiday view first.")),
            Item(ApplicationActionId.PlannerPreviousColumn, "Planner", "Previous date",
                "Select the previous date in the multiday range", Shortest(bindings.PlannerPreviousColumn),
                availability.Planner.Reason ?? (planner!.State.ViewMode == PlannerViewMode.MultiDay ? null : "Switch to multiday view first.")),
            Item(ApplicationActionId.PlannerNextColumn, "Planner", "Next date",
                "Select the next date in the multiday range", Shortest(bindings.PlannerNextColumn),
                availability.Planner.Reason ?? (planner!.State.ViewMode == PlannerViewMode.MultiDay ? null : "Switch to multiday view first.")),
            Item(ApplicationActionId.PlannerRefreshCalendar, "Planner", "Refresh calendar",
                "Connect to or refresh the primary Google Calendar", Shortest(bindings.PlannerRefreshCalendar),
                availability.Planner.Reason),
            Item(ApplicationActionId.PlannerExportSchedule, "Planner", "Export day schedule",
                "Write the selected day's schedule to the configured weekly Markdown note",
                Shortest(bindings.PlannerExportSchedule), availability.Planner.ExportReason),
            Item(ApplicationActionId.PlannerAssignOrMove, "Planner", "Assign or move todo",
                "Use the selected planner destination", Shortest(bindings.Open), availability.Planner.AssignReason),
            Item(ApplicationActionId.PlannerUnschedule, "Planner", "Unschedule todo",
                "Remove the selected assignment", Shortest(bindings.PlannerUnschedule), availability.Planner.SelectedReason),
            Item(ApplicationActionId.PlannerCreate, "Planner", "Create scheduled todo",
                "Create a todo in the selected planner destination", Shortest(bindings.CreateTodo), availability.Planner.CreateReason),
            Item(ApplicationActionId.PlannerEdit, "Planner", "Edit todo",
                "Edit fields, notes, and subtasks in one dialog", Shortest(bindings.EditTodo), availability.Planner.SelectedReason),
            Item(ApplicationActionId.PlannerEditExternal, "Planner", "Edit in $EDITOR",
                "Open the Markdown source at the selected todo", Shortest(bindings.EditTodoExternal),
                availability.Planner.SelectedReason),
            Item(ApplicationActionId.PlannerToggleCompleted, "Planner", "Toggle selected todo",
                "Change the selected checkbox", Shortest(bindings.ToggleTodo), availability.Planner.SelectedReason),
            Item(ApplicationActionId.PlannerToggleDetails, "Planner",
                planner?.State.ShowDetails == false ? "Show details" : "Hide details",
                "Show or hide the selected todo preview", Shortest(bindings.ToggleDetails), availability.Planner.Reason)
    ];

    private sealed record Availability(
        BrowserAvailability Browser,
        PlannerAvailability Planner,
        string? TimerReason,
        string? PomodoroReason);

    private sealed record BrowserAvailability(
        string? Reason, string? SelectedReason, string? MarkedReason, string? RollReason, string? CreateReason);

    private sealed record PlannerAvailability(
        string? Reason, string? CreateReason, string? SelectedReason, string? AssignReason,
        string? ExportReason, string? DayReason);

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
