namespace WolfTodo.Tui.Features.ApplicationShell.Persistence;

public interface IApplicationStateStore
{
    ApplicationSessionState Load();

    void Save(ApplicationSessionState state);
}
