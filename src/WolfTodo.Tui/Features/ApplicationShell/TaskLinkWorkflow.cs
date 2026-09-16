using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class TaskLinkWorkflow
{
    public ApplicationState Generate(
        ApplicationState state, ProjectCatalog catalog, BrowserView? browser,
        PlannerView? planner, FocusedTaskView? focused)
    {
        var identity = focused?.SelectedItem.Identity ??
            (state.Tabs.ActiveTab == new TabId("todos")
                ? browser?.SelectedTodoIdentity
                : planner?.SelectedFocusedAssignment?.Identity);
        var project = catalog.Projects.FirstOrDefault(candidate => candidate.Path == identity?.ProjectPath);
        if (identity is null || project is null)
            return Error(state, "Select a Markdown task to generate a link.");
        var code = TaskLinkCode.Generate(project.Path, identity.SourceLine);
        if (TaskLinkCode.Resolve(catalog, code, out var ambiguous) is null)
            return Error(state, ambiguous
                ? "This short task code matches multiple locations; a unique link cannot be generated."
                : "The selected Markdown task is no longer available.");
        return state with
        {
            TaskLinkPanel = new TaskLinkPanelState(
                TextBox.Create("TASK LINK", true, code, true) with { SelectionAnchor = 0 },
                false, $"{project.Title} · Line {identity.SourceLine} · Links to the current location."),
            Palette = CommandPaletteState.Closed,
            Command = ApplicationCommandState.Initial
        };
    }

    public ApplicationState Prompt(ApplicationState state) => state with
    {
        TaskLinkPanel = new TaskLinkPanelState(TextBox.Create("OPEN TASK LINK", true, "", true),
            true, "Enter the code for a configured Markdown task."),
        Palette = CommandPaletteState.Closed,
        Command = ApplicationCommandState.Initial
    };

    public ApplicationState ReducePanel(ApplicationState state, ConsoleKeyInfo key,
        TuiKeyBindings bindings, ProjectCatalog catalog, int sidebarItemCount)
    {
        var panel = state.TaskLinkPanel!;
        if (key.Key == ConsoleKey.Escape) return state with { TaskLinkPanel = null };
        // Generated codes allow selection and scrolling, but cannot be edited.
        if (!panel.IsOpening && key.Key is not (ConsoleKey.LeftArrow or ConsoleKey.RightArrow or
                ConsoleKey.Home or ConsoleKey.End) &&
            !(key.Key == ConsoleKey.A && key.Modifiers.HasFlag(ConsoleModifiers.Control))) return state;
        var transition = TextBox.Default.Reduce(panel.Input, key, bindings);
        if (transition.Outcome == TextBoxOutcome.Accepted)
            return Open(state with { TaskLinkPanel = null }, catalog, panel.Input.Text.Trim(), sidebarItemCount);
        return state with { TaskLinkPanel = panel with { Input = transition.State! } };
    }

    public ApplicationState Open(ApplicationState state, ProjectCatalog catalog, string code, int sidebarItemCount)
    {
        if (!TaskLinkCode.IsValid(code)) return Error(state, TaskLinkCode.InvalidCodeMessage);
        var resolved = TaskLinkCode.Resolve(catalog, code, out var ambiguous);
        if (resolved is null) return Error(state, ambiguous
            ? "This short task code matches multiple locations; select the task through its project instead."
            : "No task exists at the linked location in the configured projects.");
        var (project, todo) = resolved.Value;
        // An open descendant may be hidden by a completed ancestor.
        var revealCompleted = RequiresCompleted(project.Todos, todo.SourceLine, false);
        return state with
        {
            Tabs = state.Tabs with { ActiveTab = new TabId("todos") },
            FocusedTask = null,
            TaskLinkPanel = null,
            Palette = CommandPaletteState.Closed,
            Command = ApplicationCommandState.Initial,
            Browser = state.Browser with
            {
                ProjectIndex = catalog.Projects.IndexOf(project) + sidebarItemCount + 2,
                TodoIndex = 0,
                PendingTodoSelection = new TodoIdentity(project.Path, todo.SourceLine),
                Focus = BrowserFocus.Todos,
                FilterText = "", FilterDraft = "", IsFilterMode = false, IsSortMode = false,
                ShowCompleted = state.Browser.ShowCompleted || revealCompleted,
                Error = null, StatusMessage = null,
                MarkedTodos = [], MarkedTodoSnapshots = System.Collections.Immutable.ImmutableDictionary<TodoIdentity, TodoItem>.Empty
            }
        };
    }

    private static bool RequiresCompleted(IEnumerable<TodoItem> todos, int sourceLine, bool completedAncestor)
    {
        foreach (var todo in todos)
        {
            var completed = completedAncestor || todo.IsCompleted;
            if (todo.SourceLine == sourceLine) return completed;
            if (RequiresCompleted(todo.Subtasks, sourceLine, completed)) return true;
        }
        return false;
    }

    private static ApplicationState Error(ApplicationState state, string message) =>
        state with { Command = ApplicationCommandState.Initial with { Error = message } };
}
