using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationInputDispatcher(
    ApplicationInputRouter inputRouter,
    ApplicationCommandDispatcher commandDispatcher,
    ApplicationPaletteDispatcher paletteDispatcher,
    TimerWorkflow timerWorkflow,
    TaskLinkWorkflow taskLinkWorkflow,
    FocusedTaskReducer focusedTaskReducer,
    FocusedTaskWorkflow focusedTaskWorkflow,
    TabHostReducer tabReducer,
    PlannerWorkflow plannerWorkflow,
    BrowserWorkflow browserWorkflow,
    BrowserReducer browserReducer,
    ProjectTodoMutationService? mutationService)
{
    public ApplicationInputResult Handle(ApplicationInputContext context, ConsoleKeyInfo key)
    {
        var state = context.State;
        var catalog = context.Catalog;
        if (state.ReloadStatus?.DismissOnInput == true)
        {
            state = state with { ReloadStatus = null };
        }

        if (state.PomodoroCompletion is not null)
        {
            state = state with { PomodoroCompletion = null };
        }

        context = context with { State = state, Catalog = catalog };
        if (state.TaskLinkPanel is not null)
        {
            state = taskLinkWorkflow.ReducePanel(
                state,
                key,
                context.Configuration.KeyBindings,
                catalog,
                context.Configuration.SidebarItems.Length);
            return new ApplicationInputResult(state, catalog);
        }

        if (state.PomodoroPrompt is not null)
        {
            state = timerWorkflow.ReducePrompt(
                state,
                key,
                context.Configuration,
                state.Tabs.ActiveTab.Value == "todos");
            return new ApplicationInputResult(state, catalog);
        }

        var featureCapturesInput = state.FocusedTask is not null
            ? state.FocusedTask.Editor is not null
            : state.Tabs.ActiveTab.Value == "todos"
                ? state.Browser.IsFilterMode || state.Browser.IsSortMode ||
                  state.Browser.Editor is not null || state.Browser.BulkEditor is not null
                : state.Planner.CapturesInput;

        if (state.Command.IsActive ||
            (!featureCapturesInput && context.Configuration.KeyBindings.MatchesCommandMode(key)))
        {
            return commandDispatcher.Handle(context, key);
        }

        if (state.Palette.IsOpen ||
            (!featureCapturesInput && context.Configuration.KeyBindings.MatchesCommandPalette(key)))
        {
            return paletteDispatcher.Handle(context, key);
        }

        if (state.Command.Error is not null)
        {
            state = state with { Command = state.Command with { Error = null } };
        }

        if (!featureCapturesInput &&
            state.FocusedTask is null &&
            context.Configuration.KeyBindings.MatchesFocusTask(key))
        {
            state = OpenFocusedTask(state, context.BrowserView, context.PlannerView);
            return new ApplicationInputResult(state, catalog);
        }

        if (!featureCapturesInput && context.Configuration.KeyBindings.MatchesToggleTimer(key))
        {
            state = context.FocusedTaskView is not null
                ? timerWorkflow.ToggleFocused(
                    state,
                    context.FocusedTaskView,
                    context.Configuration,
                    state.Tabs.ActiveTab.Value == "todos")
                : timerWorkflow.Toggle(
                    state,
                    context.BrowserView,
                    context.PlannerView,
                    catalog,
                    context.Configuration,
                    state.Tabs.ActiveTab.Value == "todos");
            return new ApplicationInputResult(state, catalog);
        }

        if (!featureCapturesInput &&
            (context.Configuration.KeyBindings.MatchesStartPomodoro(key) ||
             context.Configuration.KeyBindings.MatchesStartUntrackedPomodoro(key)))
        {
            state = context.FocusedTaskView is not null
                ? timerWorkflow.OpenPomodoroPromptFocused(
                    state,
                    context.FocusedTaskView,
                    context.Configuration,
                    state.Tabs.ActiveTab.Value == "todos")
                : timerWorkflow.OpenPomodoroPrompt(
                    state,
                    context.BrowserView,
                    context.PlannerView,
                    catalog,
                    context.Configuration,
                    context.Configuration.KeyBindings.MatchesStartUntrackedPomodoro(key),
                    state.Tabs.ActiveTab.Value == "todos");
            return new ApplicationInputResult(state, catalog);
        }

        if (context.FocusedTaskView is not null)
        {
            var transition = focusedTaskReducer.Reduce(
                state.FocusedTask!,
                key,
                context.Configuration.KeyBindings,
                context.FocusedTaskView);
            var result = focusedTaskWorkflow.ApplyTransition(
                state,
                transition,
                catalog,
                context.Configuration,
                mutationService);
            return new ApplicationInputResult(result.State, result.Catalog);
        }

        var inputRoute = inputRouter.Route(
            featureCapturesInput,
            key,
            context.Configuration.KeyBindings);
        if (inputRoute is ApplicationInputRoute.NextTab or ApplicationInputRoute.PreviousTab)
        {
            var direction = inputRoute == ApplicationInputRoute.PreviousTab
                ? TabDirection.Previous
                : TabDirection.Next;
            var tabs = tabReducer.Move(state.Tabs, context.Tabs, direction);
            state = state with
            {
                Tabs = tabs,
                Browser = tabs.ActiveTab.Value == "todos"
                    ? state.Browser
                    : ClearBrowserMarks(state.Browser)
            };
            return new ApplicationInputResult(state, catalog);
        }

        if (state.Tabs.ActiveTab.Value == "planner")
        {
            if (!state.Planner.CapturesInput &&
                context.Configuration.KeyBindings.MatchesPlannerRefreshCalendar(key))
            {
                plannerWorkflow.Refresh(context.Configuration, state.Planner);
                return new ApplicationInputResult(state, catalog);
            }

            if (!state.Planner.CapturesInput &&
                context.Configuration.KeyBindings.MatchesPlannerExportSchedule(key))
            {
                state = plannerWorkflow.Export(state, context.PlannerView!, context.Configuration);
                return new ApplicationInputResult(state, catalog);
            }

            var transition = plannerWorkflow.Reduce(
                state.Planner,
                key,
                context.Configuration,
                context.PlannerView!);
            var plannerResult = plannerWorkflow.ApplyTransition(
                state,
                transition,
                catalog,
                context.Configuration,
                mutationService);
            return new ApplicationInputResult(plannerResult.State, plannerResult.Catalog);
        }

        var browserTransition = browserReducer.Reduce(
            state.Browser,
            key,
            context.Configuration,
            context.BrowserView!);
        var browserResult = browserWorkflow.ApplyTransition(
            state,
            browserTransition,
            catalog,
            context.Configuration,
            mutationService);
        return new ApplicationInputResult(browserResult.State, browserResult.Catalog);
    }

    internal static ApplicationState OpenFocusedTask(
        ApplicationState state,
        BrowserView? browser,
        PlannerView? planner)
    {
        if (state.Tabs.ActiveTab.Value == "todos" &&
            browser?.SelectedTodoIdentity is { } browserIdentity &&
            browser.SelectedTodo is { } browserTodo)
        {
            return state with
            {
                FocusedTask = FocusedTaskState.Create(browserIdentity, browserTodo),
                Palette = CommandPaletteState.Closed
            };
        }

        if (state.Tabs.ActiveTab.Value == "planner" &&
            planner?.SelectedFocusedAssignment is { } assignment)
        {
            return state with
            {
                FocusedTask = FocusedTaskState.Create(assignment.Identity, assignment.Todo),
                Palette = CommandPaletteState.Closed
            };
        }

        return state.Tabs.ActiveTab.Value == "todos"
            ? state with { Browser = state.Browser with { Error = "Select a todo to focus." } }
            : state with { Planner = state.Planner with { Error = "Select a todo to focus." } };
    }

    internal static BrowserState ClearBrowserMarks(BrowserState state) => state with
    {
        MarkedTodos = [],
        MarkedTodoSnapshots = System.Collections.Immutable.ImmutableDictionary<TodoIdentity, TodoItem>.Empty,
        BulkEditor = null,
        StatusMessage = null
    };
}
