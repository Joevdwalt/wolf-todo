using WolfTodo.Tui.Features.ApplicationShell.CommandPalette;
using WolfTodo.Tui.Features.ApplicationShell.Commands;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell.Input;

public sealed class ApplicationInputDispatcher(
    ApplicationInputRouter router,
    ApplicationTabInputHandler tabs,
    ApplicationCommandInputHandler commands,
    CommandPaletteInputHandler palette,
    BrowserInputHandler browser,
    PlannerInputHandler planner)
{
    public ApplicationInputResult Dispatch(ApplicationFrame frame, ConsoleKeyInfo key)
    {
        var runtime = frame.Runtime;
        var bindings = runtime.Configuration.KeyBindings;
        if (runtime.State.Command.IsActive ||
            (!frame.FeatureCapturesInput && bindings.MatchesCommandMode(key)))
        {
            return commands.Handle(frame, key);
        }

        if (runtime.State.Palette.IsOpen ||
            (!frame.FeatureCapturesInput && bindings.MatchesCommandPalette(key)))
        {
            return palette.Handle(frame, key);
        }

        runtime = ClearCommandError(runtime);
        var route = router.Route(frame.FeatureCapturesInput, key, bindings);
        if (route is ApplicationInputRoute.NextTab or ApplicationInputRoute.PreviousTab)
        {
            return new ApplicationInputResult(tabs.Move(runtime, route));
        }

        return runtime.State.Tabs.ActiveTab == ApplicationTabs.Planner
            ? new ApplicationInputResult(planner.Handle(runtime, frame.Planner!, key))
            : new ApplicationInputResult(browser.Handle(runtime, frame.Browser!, key));
    }

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
