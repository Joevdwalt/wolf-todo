namespace WolfTodo.Tui.Features.ApplicationShell;

public interface IApplicationStateStore
{
    string? LoadSelectedProjectPath();

    void SaveSelectedProjectPath(string? selectedProjectPath);
}
