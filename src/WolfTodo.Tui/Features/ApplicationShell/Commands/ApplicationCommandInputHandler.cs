using WolfTodo.Tui.Features.ApplicationShell.Actions;
using WolfTodo.Tui.Features.ApplicationShell.CommandPalette;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell.Commands;

public sealed class ApplicationCommandInputHandler(
    ApplicationCommandReducer reducer,
    ApplicationActionDispatcher actions,
    BrowserInputHandler browser)
{
    public ApplicationInputResult Handle(ApplicationFrame frame, ConsoleKeyInfo key)
    {
        var runtime = frame.Runtime;
        var transition = reducer.Reduce(
            runtime.State.Command,
            key,
            runtime.Configuration.KeyBindings);
        runtime = runtime with
        {
            State = runtime.State with { Command = transition.State }
        };
        return Execute(runtime, frame, transition);
    }

    private ApplicationInputResult Execute(
        ApplicationRuntime runtime,
        ApplicationFrame frame,
        ApplicationCommandTransition command) => command.Operation switch
    {
        ApplicationCommandOperation.Exit =>
            new ApplicationInputResult(runtime, true),
        ApplicationCommandOperation.ToggleCompleted =>
            actions.Execute(runtime, frame, ApplicationActionId.ToggleCompleted),
        ApplicationCommandOperation.OpenPalette =>
            new ApplicationInputResult(OpenPalette(runtime)),
        ApplicationCommandOperation.MoveTodoProject =>
            new ApplicationInputResult(
                browser.MoveTodoToProject(runtime, frame.Browser, command.ProjectTitle)),
        ApplicationCommandOperation.RollProjectToday =>
            new ApplicationInputResult(browser.RollProjectToday(runtime, frame.Browser)),
        _ => new ApplicationInputResult(runtime)
    };

    private static ApplicationRuntime OpenPalette(ApplicationRuntime runtime) =>
        runtime with
        {
            State = runtime.State with
            {
                Palette = CommandPaletteState.Closed with { IsOpen = true }
            }
        };
}
