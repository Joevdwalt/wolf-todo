using WolfTodo.Tui.Features.Commands;
using WolfTodo.Tui.Features.TaskFocus;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationPaletteDispatcher(
    CommandPaletteReducer paletteReducer,
    CommandPalettePresenter palettePresenter,
    ApplicationActionCatalog actionCatalog,
    ApplicationCommandDispatcher commandDispatcher,
    TimerWorkflow timerWorkflow,
    TaskLinkWorkflow taskLinkWorkflow,
    FocusedTaskReducer focusedTaskReducer,
    FocusedTaskWorkflow focusedTaskWorkflow,
    BrowserReducer browserReducer,
    BrowserWorkflow browserWorkflow,
    PlannerWorkflow plannerWorkflow,
    TabHostReducer tabReducer,
    ProjectTodoMutationService? mutationService)
{
    private static readonly IReadOnlyDictionary<ApplicationActionId, FocusedTaskAction> FocusedTaskActions =
        new Dictionary<ApplicationActionId, FocusedTaskAction>
        {
            [ApplicationActionId.ExitTaskFocus] = FocusedTaskAction.Exit,
            [ApplicationActionId.FocusEdit] = FocusedTaskAction.Edit,
            [ApplicationActionId.FocusEditExternal] = FocusedTaskAction.EditExternal,
            [ApplicationActionId.FocusToggleCompleted] = FocusedTaskAction.ToggleCompleted
        };

    private static readonly IReadOnlyDictionary<ApplicationActionId, BrowserAction> BrowserActions =
        new Dictionary<ApplicationActionId, BrowserAction>
        {
            [ApplicationActionId.BrowserFilter] = BrowserAction.Filter,
            [ApplicationActionId.BrowserSort] = BrowserAction.Sort,
            [ApplicationActionId.BrowserCreate] = BrowserAction.Create,
            [ApplicationActionId.BrowserEdit] = BrowserAction.Edit,
            [ApplicationActionId.BrowserEditExternal] = BrowserAction.EditExternal,
            [ApplicationActionId.BrowserToggleCompleted] = BrowserAction.ToggleCompleted,
            [ApplicationActionId.BrowserToggleSelection] = BrowserAction.ToggleSelection,
            [ApplicationActionId.BrowserBulkEdit] = BrowserAction.BulkEdit,
            [ApplicationActionId.BrowserClearSelection] = BrowserAction.ClearSelection,
            [ApplicationActionId.BrowserRollProjectToday] = BrowserAction.RollProjectToday,
            [ApplicationActionId.BrowserToggleDetails] = BrowserAction.ToggleDetails,
            [ApplicationActionId.BrowserJumpTop] = BrowserAction.JumpTop,
            [ApplicationActionId.BrowserJumpBottom] = BrowserAction.JumpBottom
        };

    private static readonly IReadOnlyDictionary<ApplicationActionId, PlannerAction> PlannerActions =
        new Dictionary<ApplicationActionId, PlannerAction>
        {
            [ApplicationActionId.PlannerPreviousDay] = PlannerAction.PreviousDay,
            [ApplicationActionId.PlannerNextDay] = PlannerAction.NextDay,
            [ApplicationActionId.PlannerToday] = PlannerAction.Today,
            [ApplicationActionId.PlannerToggleView] = PlannerAction.ToggleView,
            [ApplicationActionId.PlannerIncreaseRange] = PlannerAction.IncreaseRange,
            [ApplicationActionId.PlannerDecreaseRange] = PlannerAction.DecreaseRange,
            [ApplicationActionId.PlannerPreviousColumn] = PlannerAction.PreviousColumn,
            [ApplicationActionId.PlannerNextColumn] = PlannerAction.NextColumn,
            [ApplicationActionId.PlannerAssignOrMove] = PlannerAction.AssignOrMove,
            [ApplicationActionId.PlannerUnschedule] = PlannerAction.Unschedule,
            [ApplicationActionId.PlannerCreate] = PlannerAction.Create,
            [ApplicationActionId.PlannerEdit] = PlannerAction.Edit,
            [ApplicationActionId.PlannerEditExternal] = PlannerAction.EditExternal,
            [ApplicationActionId.PlannerToggleCompleted] = PlannerAction.ToggleCompleted,
            [ApplicationActionId.PlannerToggleDetails] = PlannerAction.ToggleDetails
        };

    public ApplicationInputResult Handle(ApplicationInputContext context, ConsoleKeyInfo key)
    {
        var paletteView = context.PaletteView ?? palettePresenter.CreateView(
            context.State.Palette,
            actionCatalog.Create(
                context.State.Tabs.ActiveTab.Value == "todos",
                context.BrowserView,
                context.PlannerView,
                context.Configuration.KeyBindings,
                context.Configuration.Planner.Export is not null,
                context.Configuration.Timer is not null,
                context.State.Timer is not null,
                context.FocusedTaskView));
        var transition = paletteReducer.Reduce(
            context.State.Palette,
            key,
            context.Configuration.KeyBindings,
            paletteView);
        var state = context.State with { Palette = transition.State };
        return transition.Action is { } action
            ? HandleAction(state, context, action)
            : new ApplicationInputResult(state, context.Catalog);
    }

    private ApplicationInputResult HandleAction(
        ApplicationState state,
        ApplicationInputContext context,
        ApplicationActionId action)
    {
        var globalResult = HandleGlobalAction(state, context, action);
        if (globalResult is not null)
        {
            return globalResult;
        }

        var focusedResult = HandleFocusedTaskAction(state, context, action);
        if (focusedResult is not null)
        {
            return focusedResult;
        }

        if (action == ApplicationActionId.ToggleCompleted)
        {
            state = state with
            {
                Browser = state.Browser with
                {
                    ShowCompleted = !state.Browser.ShowCompleted,
                    TodoIndex = 0,
                    PendingTodoSelection = null,
                    Error = null
                }
            };
            return new ApplicationInputResult(state, context.Catalog);
        }

        if (action is ApplicationActionId.NextTab or ApplicationActionId.PreviousTab)
        {
            var direction = action == ApplicationActionId.NextTab ? TabDirection.Next : TabDirection.Previous;
            var tabs = tabReducer.Move(state.Tabs, context.Tabs, direction);
            state = state with
            {
                Tabs = tabs,
                Browser = tabs.ActiveTab.Value == "todos"
                    ? state.Browser
                    : ApplicationInputDispatcher.ClearBrowserMarks(state.Browser)
            };
            return new ApplicationInputResult(state, context.Catalog);
        }

        return state.Tabs.ActiveTab.Value == "todos"
            ? HandleBrowserAction(state, context, action)
            : HandlePlannerAction(state, context, action);
    }

    private ApplicationInputResult? HandleGlobalAction(
        ApplicationState state,
        ApplicationInputContext context,
        ApplicationActionId action)
    {
        if (action == ApplicationActionId.Exit)
        {
            state = timerWorkflow.Stop(
                state,
                context.Configuration,
                state.Tabs.ActiveTab.Value == "todos");
            return new ApplicationInputResult(state, context.Catalog, state.Timer is null);
        }

        return action switch
        {
            ApplicationActionId.GenerateTaskLink => new ApplicationInputResult(
                taskLinkWorkflow.Generate(
                    state,
                    context.Catalog,
                    context.BrowserView,
                    context.PlannerView,
                    context.FocusedTaskView),
                context.Catalog),
            ApplicationActionId.OpenTaskLink => new ApplicationInputResult(
                taskLinkWorkflow.Prompt(state), context.Catalog),
            ApplicationActionId.OpenConfiguration => new ApplicationInputResult(
                commandDispatcher.OpenConfiguration(state), context.Catalog),
            ApplicationActionId.ToggleTimer or
                ApplicationActionId.StartPomodoro or
                ApplicationActionId.StartUntrackedPomodoro => HandleTimerAction(state, context, action),
            ApplicationActionId.FocusSelectedTask => new ApplicationInputResult(
                ApplicationInputDispatcher.OpenFocusedTask(state, context.BrowserView, context.PlannerView),
                context.Catalog),
            _ => null
        };
    }

    private ApplicationInputResult HandleTimerAction(
        ApplicationState state,
        ApplicationInputContext context,
        ApplicationActionId action)
    {
        var isTodos = state.Tabs.ActiveTab.Value == "todos";
        if (action == ApplicationActionId.ToggleTimer)
        {
            state = context.FocusedTaskView is { } focusedTask
                ? timerWorkflow.ToggleFocused(state, focusedTask, context.Configuration, isTodos)
                : timerWorkflow.Toggle(
                    state,
                    context.BrowserView,
                    context.PlannerView,
                    context.Catalog,
                    context.Configuration,
                    isTodos);
        }
        else
        {
            state = context.FocusedTaskView is { } focusedTask
                ? timerWorkflow.OpenPomodoroPromptFocused(state, focusedTask, context.Configuration, isTodos)
                : timerWorkflow.OpenPomodoroPrompt(
                    state,
                    context.BrowserView,
                    context.PlannerView,
                    context.Catalog,
                    context.Configuration,
                    action == ApplicationActionId.StartUntrackedPomodoro,
                    isTodos);
        }

        return new ApplicationInputResult(state, context.Catalog);
    }

    private ApplicationInputResult? HandleFocusedTaskAction(
        ApplicationState state,
        ApplicationInputContext context,
        ApplicationActionId action)
    {
        if (context.FocusedTaskView is null)
        {
            return null;
        }

        if (!FocusedTaskActions.TryGetValue(action, out var focusedAction))
        {
            return new ApplicationInputResult(state, context.Catalog);
        }

        var transition = focusedTaskReducer.ReduceAction(
            state.FocusedTask!,
            focusedAction,
            context.FocusedTaskView);
        var result = focusedTaskWorkflow.ApplyTransition(
            state,
            transition,
            context.Catalog,
            context.Configuration,
            mutationService);
        return new ApplicationInputResult(result.State, result.Catalog);
    }

    private ApplicationInputResult HandleBrowserAction(
        ApplicationState state,
        ApplicationInputContext context,
        ApplicationActionId action)
    {
        if (!BrowserActions.TryGetValue(action, out var browserAction))
        {
            return new ApplicationInputResult(state, context.Catalog);
        }

        var transition = browserReducer.ReduceAction(state.Browser, browserAction, context.BrowserView!);
        var result = browserWorkflow.ApplyTransition(
            state,
            transition,
            context.Catalog,
            context.Configuration,
            mutationService);
        return new ApplicationInputResult(result.State, result.Catalog);
    }

    private ApplicationInputResult HandlePlannerAction(
        ApplicationState state,
        ApplicationInputContext context,
        ApplicationActionId action)
    {
        if (action == ApplicationActionId.PlannerRefreshCalendar)
        {
            plannerWorkflow.Refresh(context.Configuration, state.Planner);
            return new ApplicationInputResult(state, context.Catalog);
        }

        if (action == ApplicationActionId.PlannerExportSchedule)
        {
            return new ApplicationInputResult(
                plannerWorkflow.Export(state, context.PlannerView!, context.Configuration),
                context.Catalog);
        }

        if (!PlannerActions.TryGetValue(action, out var plannerAction))
        {
            return new ApplicationInputResult(state, context.Catalog);
        }

        var transition = plannerWorkflow.ReduceAction(
            state.Planner,
            plannerAction,
            context.Configuration,
            context.PlannerView!);
        var result = plannerWorkflow.ApplyTransition(
            state,
            transition,
            context.Catalog,
            context.Configuration,
            mutationService);
        return new ApplicationInputResult(result.State, result.Catalog);
    }
}
