using WolfTodo.Tui.Features.ApplicationShell.Actions;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class PlannerInputHandler(
    DayPlannerReducer reducer,
    PlannerCalendarAgendaCache calendarCache,
    PlannerTransitionExecutor transitions)
{
    private static readonly IReadOnlyDictionary<ApplicationActionId, PlannerAction> Actions =
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

    public ApplicationRuntime Handle(
        ApplicationRuntime runtime,
        PlannerView view,
        ConsoleKeyInfo key)
    {
        var bindings = runtime.Configuration.KeyBindings;
        if (!runtime.State.Planner.CapturesInput &&
            bindings.MatchesPlannerRefreshCalendar(key))
        {
            RefreshCalendar(runtime);
            return runtime;
        }

        if (!runtime.State.Planner.CapturesInput &&
            bindings.MatchesPlannerExportSchedule(key))
        {
            return transitions.ExportDaySchedule(runtime, view);
        }

        return transitions.Apply(
            runtime,
            reducer.Reduce(
                runtime.State.Planner,
                key,
                bindings,
                view,
                runtime.Configuration.Planner.DefaultDuration));
    }

    public ApplicationRuntime HandleAction(
        ApplicationRuntime runtime,
        PlannerView view,
        ApplicationActionId action)
    {
        if (action == ApplicationActionId.PlannerRefreshCalendar)
        {
            RefreshCalendar(runtime);
            return runtime;
        }

        if (action == ApplicationActionId.PlannerExportSchedule)
        {
            return transitions.ExportDaySchedule(runtime, view);
        }

        return Actions.TryGetValue(action, out var plannerAction)
            ? transitions.Apply(
                runtime,
                reducer.ReduceAction(
                    runtime.State.Planner,
                    plannerAction,
                    view,
                    runtime.Configuration.Planner.DefaultDuration))
            : runtime;
    }

    private void RefreshCalendar(ApplicationRuntime runtime) =>
        calendarCache.Refresh(
            runtime.Configuration.GoogleCalendar,
            runtime.State.Planner.SelectedDate);
}
