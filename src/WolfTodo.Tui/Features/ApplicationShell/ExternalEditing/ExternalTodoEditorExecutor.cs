using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Features.ApplicationShell.ExternalEditing;

public sealed class ExternalTodoEditorExecutor(
    ITerminalUi terminalUi,
    IExternalEditorLauncher? launcher)
{
    public ExternalEditorResult Open(string? path, TodoIdentity? identity)
    {
        if (launcher is null || path is null || identity is null)
        {
            return ExternalEditorResult.Failure(false, "External editing is unavailable.");
        }

        terminalUi.SuspendForExternalProcess();
        try
        {
            return launcher.Open(path, identity.SourceLine);
        }
        finally
        {
            terminalUi.ResumeAfterExternalProcess();
        }
    }
}
