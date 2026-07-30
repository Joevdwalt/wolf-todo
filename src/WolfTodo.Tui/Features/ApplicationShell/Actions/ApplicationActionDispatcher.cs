using WolfTodo.Tui.Features.ApplicationShell.Input;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell.Actions;

public sealed class ApplicationActionDispatcher(
    ApplicationTabInputHandler tabs,
    BrowserInputHandler browser,
    PlannerInputHandler planner)
{
    public ApplicationInputResult Execute(
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
            return new ApplicationInputResult(tabs.Move(runtime, TabRoute(action)));
        }

        return runtime.State.Tabs.ActiveTab == ApplicationTabs.Todos
            ? new ApplicationInputResult(browser.HandleAction(runtime, frame.Browser!, action))
            : new ApplicationInputResult(planner.HandleAction(runtime, frame.Planner!, action));
    }

    private static ApplicationInputRoute TabRoute(ApplicationActionId action) =>
        action == ApplicationActionId.NextTab
            ? ApplicationInputRoute.NextTab
            : ApplicationInputRoute.PreviousTab;

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
}
