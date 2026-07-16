namespace WolfTodo.Tui.Features.ApplicationShell;

public interface IExternalEditorLauncher
{
    ExternalEditorResult Open(string projectPath, int sourceLine);
}
