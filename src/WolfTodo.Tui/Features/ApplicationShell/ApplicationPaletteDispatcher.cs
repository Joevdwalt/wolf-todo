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
        var catalog = context.Catalog;
        if (transition.Action is null)
        {
            return new ApplicationInputResult(state, catalog);
        }

        var action = transition.Action.Value;
        if (action == ApplicationActionId.Exit)
        {
            state = timerWorkflow.Stop(
                state,
                context.Configuration,
                state.Tabs.ActiveTab.Value == "todos");
            return new ApplicationInputResult(state, catalog, state.Timer is null);
        }

        if (action == ApplicationActionId.GenerateTaskLink)
        {
            return new ApplicationInputResult(
                taskLinkWorkflow.Generate(
                    state,
                    catalog,
                    context.BrowserView,
                    context.PlannerView,
                    context.FocusedTaskView),
                catalog);
        }

        if (action == ApplicationActionId.OpenTaskLink)
        {
            return new ApplicationInputResult(
                taskLinkWorkflow.Prompt(state),
                catalog);
        }

        if (action == ApplicationActionId.OpenConfiguration)
        {
            return new ApplicationInputResult(commandDispatcher.OpenConfiguration(state), catalog);
        }

        if (action == ApplicationActionId.ToggleTimer)
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

        if (action is ApplicationActionId.StartPomodoro or ApplicationActionId.StartUntrackedPomodoro)
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
                    action == ApplicationActionId.StartUntrackedPomodoro,
                    state.Tabs.ActiveTab.Value == "todos");
            return new ApplicationInputResult(state, catalog);
        }

        if (action == ApplicationActionId.FocusSelectedTask)
        {
            return new ApplicationInputResult(
                ApplicationInputDispatcher.OpenFocusedTask(state, context.BrowserView, context.PlannerView),
                catalog);
        }

        if (context.FocusedTaskView is not null)
        {
            var focusAction = action switch
            {
                ApplicationActionId.ExitTaskFocus => FocusedTaskAction.Exit,
                ApplicationActionId.FocusEdit => FocusedTaskAction.Edit,
                ApplicationActionId.FocusEditExternal => FocusedTaskAction.EditExternal,
                ApplicationActionId.FocusToggleCompleted => FocusedTaskAction.ToggleCompleted,
                _ => (FocusedTaskAction?)null
            };
            if (focusAction is not null)
            {
                var transitionToApply = focusedTaskReducer.ReduceAction(
                    state.FocusedTask!,
                    focusAction.Value,
                    context.FocusedTaskView);
                var result = focusedTaskWorkflow.ApplyTransition(
                    state,
                    transitionToApply,
                    catalog,
                    context.Configuration,
                    mutationService);
                return new ApplicationInputResult(result.State, result.Catalog);
            }

            return new ApplicationInputResult(state, catalog);
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
            return new ApplicationInputResult(state, catalog);
        }

        if (action is ApplicationActionId.NextTab or ApplicationActionId.PreviousTab)
        {
            var direction = action == ApplicationActionId.NextTab
                ? TabDirection.Next
                : TabDirection.Previous;
            var tabs = tabReducer.Move(state.Tabs, context.Tabs, direction);
            state = state with
            {
                Tabs = tabs,
                Browser = tabs.ActiveTab.Value == "todos"
                    ? state.Browser
                    : ApplicationInputDispatcher.ClearBrowserMarks(state.Browser)
            };
            return new ApplicationInputResult(state, catalog);
        }

        if (state.Tabs.ActiveTab.Value == "todos")
        {
            var browserAction = action switch
            {
                ApplicationActionId.BrowserFilter => BrowserAction.Filter,
                ApplicationActionId.BrowserSort => BrowserAction.Sort,
                ApplicationActionId.BrowserCreate => BrowserAction.Create,
                ApplicationActionId.BrowserEdit => BrowserAction.Edit,
                ApplicationActionId.BrowserEditExternal => BrowserAction.EditExternal,
                ApplicationActionId.BrowserToggleCompleted => BrowserAction.ToggleCompleted,
                ApplicationActionId.BrowserToggleSelection => BrowserAction.ToggleSelection,
                ApplicationActionId.BrowserBulkEdit => BrowserAction.BulkEdit,
                ApplicationActionId.BrowserClearSelection => BrowserAction.ClearSelection,
                ApplicationActionId.BrowserRollProjectToday => BrowserAction.RollProjectToday,
                ApplicationActionId.BrowserToggleDetails => BrowserAction.ToggleDetails,
                ApplicationActionId.BrowserJumpTop => BrowserAction.JumpTop,
                ApplicationActionId.BrowserJumpBottom => BrowserAction.JumpBottom,
                _ => (BrowserAction?)null
            };
            if (browserAction is not null)
            {
                var browserTransition = browserReducer.ReduceAction(
                    state.Browser,
                    browserAction.Value,
                    context.BrowserView!);
                var result = browserWorkflow.ApplyTransition(
                    state,
                    browserTransition,
                    catalog,
                    context.Configuration,
                    mutationService);
                return new ApplicationInputResult(result.State, result.Catalog);
            }

            return new ApplicationInputResult(state, catalog);
        }

        if (action == ApplicationActionId.PlannerRefreshCalendar)
        {
            plannerWorkflow.Refresh(context.Configuration, state.Planner);
            return new ApplicationInputResult(state, catalog);
        }

        if (action == ApplicationActionId.PlannerExportSchedule)
        {
            return new ApplicationInputResult(
                plannerWorkflow.Export(state, context.PlannerView!, context.Configuration),
                catalog);
        }

        var plannerAction = action switch
        {
            ApplicationActionId.PlannerPreviousDay => PlannerAction.PreviousDay,
            ApplicationActionId.PlannerNextDay => PlannerAction.NextDay,
            ApplicationActionId.PlannerToday => PlannerAction.Today,
            ApplicationActionId.PlannerAssignOrMove => PlannerAction.AssignOrMove,
            ApplicationActionId.PlannerUnschedule => PlannerAction.Unschedule,
            ApplicationActionId.PlannerCreate => PlannerAction.Create,
            ApplicationActionId.PlannerEdit => PlannerAction.Edit,
            ApplicationActionId.PlannerEditExternal => PlannerAction.EditExternal,
            ApplicationActionId.PlannerToggleCompleted => PlannerAction.ToggleCompleted,
            ApplicationActionId.PlannerToggleDetails => PlannerAction.ToggleDetails,
            _ => (PlannerAction?)null
        };
        if (plannerAction is null)
        {
            return new ApplicationInputResult(state, catalog);
        }

        var plannerTransition = plannerWorkflow.ReduceAction(
            state.Planner,
            plannerAction.Value,
            context.Configuration,
            context.PlannerView!);
        var plannerResult = plannerWorkflow.ApplyTransition(
            state,
            plannerTransition,
            catalog,
            context.Configuration,
            mutationService);
        return new ApplicationInputResult(plannerResult.State, plannerResult.Catalog);
    }

}
