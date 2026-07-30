using WolfTodo.Tui.Features.ApplicationShell.Input;
using WolfTodo.Tui.Features.ApplicationShell.Persistence;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Features.ApplicationShell.Runtime;

public sealed class ApplicationSession(
    ITerminalUi terminalUi,
    IApplicationStateStore stateStore,
    ApplicationFrameRenderer frameRenderer,
    ApplicationInputDispatcher inputDispatcher,
    string logo)
{
    public int Run(ApplicationRuntime initialRuntime)
    {
        var runtime = initialRuntime;
        terminalUi.SetCursorVisible(false);
        try
        {
            ShowSplash(runtime);
            return RunLoop(runtime);
        }
        finally
        {
            Save(runtime);
            terminalUi.SetCursorVisible(true);
        }

        int RunLoop(ApplicationRuntime current)
        {
            while (true)
            {
                var frame = frameRenderer.Render(current);
                current = frame.Runtime;
                runtime = current;
                var key = ReadKey(frame.ReadTimeout);
                if (key is null)
                {
                    continue;
                }

                var result = inputDispatcher.Dispatch(frame with { Runtime = current }, key.Value);
                current = result.Runtime;
                runtime = current;
                if (result.ShouldExit)
                {
                    return 0;
                }
            }
        }
    }

    private void ShowSplash(ApplicationRuntime runtime)
    {
        terminalUi.ShowSplash(logo, runtime.Configuration.Theme);
        terminalUi.ReadKey();
    }

    private ConsoleKeyInfo? ReadKey(TimeSpan? timeout) =>
        timeout is null ? terminalUi.ReadKey() : terminalUi.ReadKey(timeout.Value);

    private void Save(ApplicationRuntime runtime) =>
        stateStore.Save(new ApplicationSessionState(
            runtime.SelectedProjectPath,
            runtime.State.Browser.Sort));
}
