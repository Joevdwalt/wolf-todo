namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class NullApplicationFileChangeMonitor : IApplicationFileChangeMonitor
{
    public static NullApplicationFileChangeMonitor Instance { get; } = new();

    private NullApplicationFileChangeMonitor()
    {
    }

    public void WatchProjectFiles(IEnumerable<string> paths)
    {
    }

    public ApplicationFileChanges Poll() => ApplicationFileChanges.None;

    public void Dispose()
    {
    }
}
