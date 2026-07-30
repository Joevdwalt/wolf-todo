using WolfTodo.Tui.Features.ApplicationShell.Actions;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;

namespace WolfTodo.Tui.Features.ApplicationShell.CommandPalette;

public sealed class CommandPaletteInputHandler(
    CommandPaletteReducer reducer,
    CommandPalettePresenter presenter,
    ApplicationActionCatalog catalog,
    ApplicationActionDispatcher actions)
{
    public ApplicationInputResult Handle(ApplicationFrame frame, ConsoleKeyInfo key)
    {
        var runtime = frame.Runtime;
        var palette = frame.Palette ?? CreateView(frame);
        var transition = reducer.Reduce(
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
            : actions.Execute(runtime, frame, transition.Action.Value);
    }

    private CommandPaletteView CreateView(ApplicationFrame frame) =>
        presenter.CreateView(
            frame.Runtime.State.Palette,
            catalog.Create(
                frame.Runtime.State.Tabs.ActiveTab == ApplicationTabs.Todos,
                frame.Browser,
                frame.Planner,
                frame.Runtime.Configuration.KeyBindings,
                frame.Runtime.Configuration.Planner.Export is not null));
}
