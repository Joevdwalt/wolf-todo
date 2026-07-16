namespace WolfTodo.Tui.Features.ApplicationShell;

public interface IApplicationStateStore
{
    ApplicationSessionState Load();

    void Save(ApplicationSessionState state);
}
