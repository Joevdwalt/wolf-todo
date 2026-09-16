namespace WolfTodo.Tui.Features.ApplicationShell;

public interface IApplicationFileChangeMonitor : IDisposable
{
    void WatchProjectFiles(IEnumerable<string> paths);

    ApplicationFileChanges Poll();
}
