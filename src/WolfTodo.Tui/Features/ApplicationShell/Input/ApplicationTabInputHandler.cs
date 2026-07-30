using WolfTodo.Tui.Features.ApplicationShell.Runtime;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell.Input;

public sealed class ApplicationTabInputHandler(TabHostReducer reducer)
{
    public ApplicationRuntime Move(
        ApplicationRuntime runtime,
        ApplicationInputRoute route) =>
        runtime with
        {
            State = runtime.State with
            {
                Tabs = reducer.Move(
                    runtime.State.Tabs,
                    ApplicationTabs.All,
                    route == ApplicationInputRoute.PreviousTab
                        ? TabDirection.Previous
                        : TabDirection.Next)
            }
        };
}
