using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Actions;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class BrowserInputHandler(
    BrowserReducer reducer,
    BrowserTransitionExecutor transitions)
{
    private static readonly IReadOnlyDictionary<ApplicationActionId, BrowserAction> Actions =
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

    public ApplicationRuntime Handle(
        ApplicationRuntime runtime,
        BrowserView view,
        ConsoleKeyInfo key) =>
        transitions.Apply(
            runtime,
            reducer.Reduce(runtime.State.Browser, key, runtime.Configuration, view));

    public ApplicationRuntime HandleAction(
        ApplicationRuntime runtime,
        BrowserView view,
        ApplicationActionId action) =>
        Actions.TryGetValue(action, out var browserAction)
            ? transitions.Apply(
                runtime,
                reducer.ReduceAction(runtime.State.Browser, browserAction, view))
            : runtime;

    public ApplicationRuntime MoveTodoToProject(
        ApplicationRuntime runtime,
        BrowserView? view,
        string? targetTitle) =>
        transitions.MoveTodoToProject(runtime, view, targetTitle);

    public ApplicationRuntime RollProjectToday(
        ApplicationRuntime runtime,
        BrowserView? view)
    {
        if (runtime.State.Tabs.ActiveTab != ApplicationTabs.Todos || view is null)
        {
            return CommandFailure(
                runtime,
                "Open Todos and select a project before rolling tasks to today.");
        }

        return transitions.Apply(
            runtime,
            reducer.ReduceAction(
                runtime.State.Browser,
                BrowserAction.RollProjectToday,
                view));
    }

    private static ApplicationRuntime CommandFailure(
        ApplicationRuntime runtime,
        string error) =>
        runtime with
        {
            State = runtime.State with
            {
                Command = runtime.State.Command with { Error = error }
            }
        };
}
