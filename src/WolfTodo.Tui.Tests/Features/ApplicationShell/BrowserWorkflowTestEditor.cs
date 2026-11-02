using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

internal sealed class BrowserWorkflowTestEditor(ExternalEditorResult result, Action? onOpen = null) : IExternalEditorLauncher
{
    public string? Path { get; private set; }
    public int Line { get; private set; }

    public ExternalEditorResult Open(string projectPath, int sourceLine)
    {
        Path = projectPath;
        Line = sourceLine;
        onOpen?.Invoke();
        return result;
    }
}
