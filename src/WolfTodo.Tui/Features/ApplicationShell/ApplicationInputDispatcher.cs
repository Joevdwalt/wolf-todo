using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationInputDispatcher(
    ApplicationInputRouter inputRouter,
    TabHostReducer tabReducer,
    BrowserReducer browserReducer,
    DayPlannerReducer plannerReducer,
    ApplicationCommandReducer commandReducer,
    CommandPaletteReducer paletteReducer,
    CommandPalettePresenter palettePresenter,
    ApplicationActionCatalog actionCatalog,
    PlannerCalendarAgendaCache calendarCache,
    ApplicationTransitionExecutor transitions)
{
    private static readonly IReadOnlyDictionary<ApplicationActionId, BrowserAction> BrowserActions =
        new Dictionary<ApplicationActionId, BrowserAction>
        {
            [ApplicationActionId.BrowserFilter] = BrowserAction.Filter,
            [ApplicationActionId.BrowserSort] = BrowserAction.Sort,
            [ApplicationActionId.BrowserCreate] = BrowserAction.Create,
            [ApplicationActionId.BrowserEdit] = BrowserAction.Edit,
            [ApplicationActionId.BrowserEditExternal] = BrowserAction.EditExternal,
            [ApplicationActionId.BrowserToggleCompleted] = BrowserAction.ToggleCompleted,
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
            [ApplicationActionId.PlannerAssignOrMove] = PlannerAction.AssignOrMove,
            [ApplicationActionId.PlannerUnschedule] = PlannerAction.Unschedule,
            [ApplicationActionId.PlannerCreate] = PlannerAction.Create,
            [ApplicationActionId.PlannerEdit] = PlannerAction.Edit,
            [ApplicationActionId.PlannerEditExternal] = PlannerAction.EditExternal,
            [ApplicationActionId.PlannerToggleCompleted] = PlannerAction.ToggleCompleted,
            [ApplicationActionId.PlannerToggleDetails] = PlannerAction.ToggleDetails
        };

    public ApplicationInputResult Dispatch(ApplicationFrame frame, ConsoleKeyInfo key)
    {
        var runtime = frame.Runtime;
        var bindings = runtime.Configuration.KeyBindings;
        if (runtime.State.Command.IsActive ||
            (!frame.FeatureCapturesInput && bindings.MatchesCommandMode(key)))
        {
            return HandleCommand(frame, key);
        }

        if (runtime.State.Palette.IsOpen ||
            (!frame.FeatureCapturesInput && bindings.MatchesCommandPalette(key)))
        {
            return HandlePalette(frame, key);
        }

        runtime = ClearCommandError(runtime);
        var route = inputRouter.Route(frame.FeatureCapturesInput, key, bindings);
        if (route is ApplicationInputRoute.NextTab or ApplicationInputRoute.PreviousTab)
        {
            return new ApplicationInputResult(MoveTab(runtime, route));
        }

        return runtime.State.Tabs.ActiveTab == ApplicationTabs.Planner
            ? HandlePlannerInput(runtime, frame.Planner!, key)
            : HandleBrowserInput(runtime, frame.Browser!, key);
    }

    private ApplicationInputResult HandleCommand(ApplicationFrame frame, ConsoleKeyInfo key)
    {
        var runtime = frame.Runtime;
        var transition = commandReducer.Reduce(
            runtime.State.Command,
            key,
            runtime.Configuration.KeyBindings);
        runtime = runtime with
        {
            State = runtime.State with { Command = transition.State }
        };
        return ExecuteCommand(runtime, frame.Browser, transition);
    }

    private ApplicationInputResult ExecuteCommand(
        ApplicationRuntime runtime,
        BrowserView? browser,
        ApplicationCommandTransition command) => command.Operation switch
    {
        ApplicationCommandOperation.Exit => new ApplicationInputResult(runtime, true),
        ApplicationCommandOperation.ToggleCompleted =>
            new ApplicationInputResult(ToggleCompleted(runtime)),
        ApplicationCommandOperation.OpenPalette =>
            new ApplicationInputResult(OpenPalette(runtime)),
        ApplicationCommandOperation.MoveTodoProject =>
            new ApplicationInputResult(
                transitions.MoveTodoToProject(runtime, browser, command.ProjectTitle)),
        ApplicationCommandOperation.RollProjectToday =>
            new ApplicationInputResult(RollProjectToday(runtime, browser)),
        _ => new ApplicationInputResult(runtime)
    };

    private ApplicationInputResult HandlePalette(ApplicationFrame frame, ConsoleKeyInfo key)
    {
        var runtime = frame.Runtime;
        var palette = frame.Palette ?? CreatePalette(frame);
        var transition = paletteReducer.Reduce(
            runtime.State.Palette,
            key,
            runtime.Configuration.KeyBindings,
            palette);
        runtime = runtime with
        {
            State = runtime.State with { Palette = transition.State }
        };
        return transition.Action is null
            ? new ApplicationInputResult(runtime)
            : ExecuteAction(runtime, frame, transition.Action.Value);
    }

    private ApplicationInputResult ExecuteAction(
        ApplicationRuntime runtime,
        ApplicationFrame frame,
        ApplicationActionId action)
    {
        if (action == ApplicationActionId.Exit)
        {
            return new ApplicationInputResult(runtime, true);
        }

        if (action == ApplicationActionId.ToggleCompleted)
        {
            return new ApplicationInputResult(ToggleCompleted(runtime));
        }

        if (action is ApplicationActionId.NextTab or ApplicationActionId.PreviousTab)
        {
            return new ApplicationInputResult(MoveTab(
                runtime,
                action == ApplicationActionId.NextTab
                    ? ApplicationInputRoute.NextTab
                    : ApplicationInputRoute.PreviousTab));
        }

        return runtime.State.Tabs.ActiveTab == ApplicationTabs.Todos
            ? ExecuteBrowserAction(runtime, frame.Browser!, action)
            : ExecutePlannerAction(runtime, frame.Planner!, action);
    }

    private ApplicationInputResult ExecuteBrowserAction(
        ApplicationRuntime runtime,
        BrowserView view,
        ApplicationActionId action)
    {
        if (!BrowserActions.TryGetValue(action, out var browserAction))
        {
            return new ApplicationInputResult(runtime);
        }

        return new ApplicationInputResult(transitions.ApplyBrowser(
            runtime,
            browserReducer.ReduceAction(runtime.State.Browser, browserAction, view)));
    }

    private ApplicationInputResult ExecutePlannerAction(
        ApplicationRuntime runtime,
        PlannerView view,
        ApplicationActionId action)
    {
        if (action == ApplicationActionId.PlannerRefreshCalendar)
        {
            RefreshCalendar(runtime);
            return new ApplicationInputResult(runtime);
        }

        if (action == ApplicationActionId.PlannerExportSchedule)
        {
            return new ApplicationInputResult(transitions.ExportDaySchedule(runtime, view));
        }

        if (!PlannerActions.TryGetValue(action, out var plannerAction))
        {
            return new ApplicationInputResult(runtime);
        }

        return new ApplicationInputResult(transitions.ApplyPlanner(
            runtime,
            plannerReducer.ReduceAction(
                runtime.State.Planner,
                plannerAction,
                view,
                runtime.Configuration.Planner.DefaultDuration)));
    }

    private ApplicationInputResult HandlePlannerInput(
        ApplicationRuntime runtime,
        PlannerView view,
        ConsoleKeyInfo key)
    {
        var bindings = runtime.Configuration.KeyBindings;
        if (!runtime.State.Planner.CapturesInput &&
            bindings.MatchesPlannerRefreshCalendar(key))
        {
            RefreshCalendar(runtime);
            return new ApplicationInputResult(runtime);
        }

        if (!runtime.State.Planner.CapturesInput &&
            bindings.MatchesPlannerExportSchedule(key))
        {
            return new ApplicationInputResult(transitions.ExportDaySchedule(runtime, view));
        }

        return new ApplicationInputResult(transitions.ApplyPlanner(
            runtime,
            plannerReducer.Reduce(
                runtime.State.Planner,
                key,
                bindings,
                view,
                runtime.Configuration.Planner.DefaultDuration)));
    }

    private ApplicationInputResult HandleBrowserInput(
        ApplicationRuntime runtime,
        BrowserView view,
        ConsoleKeyInfo key) =>
        new(transitions.ApplyBrowser(
            runtime,
            browserReducer.Reduce(runtime.State.Browser, key, runtime.Configuration, view)));

    private ApplicationRuntime RollProjectToday(
        ApplicationRuntime runtime,
        BrowserView? view)
    {
        if (runtime.State.Tabs.ActiveTab != ApplicationTabs.Todos || view is null)
        {
            return runtime with
            {
                State = runtime.State with
                {
                    Command = runtime.State.Command with
                    {
                        Error = "Open Todos and select a project before rolling tasks to today."
                    }
                }
            };
        }

        return transitions.ApplyBrowser(
            runtime,
            browserReducer.ReduceAction(
                runtime.State.Browser,
                BrowserAction.RollProjectToday,
                view));
    }

    private CommandPaletteView CreatePalette(ApplicationFrame frame) =>
        palettePresenter.CreateView(
            frame.Runtime.State.Palette,
            actionCatalog.Create(
                frame.Runtime.State.Tabs.ActiveTab == ApplicationTabs.Todos,
                frame.Browser,
                frame.Planner,
                frame.Runtime.Configuration.KeyBindings,
                frame.Runtime.Configuration.Planner.Export is not null));

    private ApplicationRuntime MoveTab(
        ApplicationRuntime runtime,
        ApplicationInputRoute route) =>
        runtime with
        {
            State = runtime.State with
            {
                Tabs = tabReducer.Move(
                    runtime.State.Tabs,
                    ApplicationTabs.All,
                    route == ApplicationInputRoute.PreviousTab
                        ? TabDirection.Previous
                        : TabDirection.Next)
            }
        };

    private void RefreshCalendar(ApplicationRuntime runtime) =>
        calendarCache.Refresh(
            runtime.Configuration.GoogleCalendar,
            runtime.State.Planner.SelectedDate);

    private static ApplicationRuntime ToggleCompleted(ApplicationRuntime runtime) =>
        runtime with
        {
            State = runtime.State with
            {
                Browser = runtime.State.Browser with
                {
                    ShowCompleted = !runtime.State.Browser.ShowCompleted,
                    TodoIndex = 0,
                    PendingTodoSelection = null,
                    Error = null
                }
            }
        };

    private static ApplicationRuntime OpenPalette(ApplicationRuntime runtime) =>
        runtime with
        {
            State = runtime.State with
            {
                Palette = CommandPaletteState.Closed with { IsOpen = true }
            }
        };

    private static ApplicationRuntime ClearCommandError(ApplicationRuntime runtime) =>
        runtime.State.Command.Error is null
            ? runtime
            : runtime with
            {
                State = runtime.State with
                {
                    Command = runtime.State.Command with { Error = null }
                }
            };
}
