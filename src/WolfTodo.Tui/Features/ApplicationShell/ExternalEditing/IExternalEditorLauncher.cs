namespace WolfTodo.Tui.Features.ApplicationShell.ExternalEditing;

public interface IExternalEditorLauncher
{
    ExternalEditorResult Open(string projectPath, int sourceLine);
}
