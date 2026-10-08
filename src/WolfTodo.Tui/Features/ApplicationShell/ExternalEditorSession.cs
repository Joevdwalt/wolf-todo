namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ExternalEditorSession(
    ITerminalUi terminalUi,
    IExternalEditorLauncher? launcher)
{
    public ExternalEditorResult Open(string path, int sourceLine)
    {
        if (launcher is null)
        {
            return new ExternalEditorResult(false, "External editing is unavailable.");
        }

        terminalUi.SuspendForExternalProcess();
        try
        {
            return launcher.Open(path, sourceLine);
        }
        finally
        {
            terminalUi.ResumeAfterExternalProcess();
        }
    }
}
