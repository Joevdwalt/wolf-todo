using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Infrastructure.Files;

public sealed class PhysicalApplicationFileChangeMonitor : IApplicationFileChangeMonitor
{
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(150);
    private readonly object gate = new();
    private readonly string configurationPath;
    private readonly Func<DateTime> utcNowProvider;
    private readonly StringComparer pathComparer;
    private readonly List<FileSystemWatcher> watchers = [];
    private HashSet<string> projectPaths;
    private ApplicationFileChanges pending = ApplicationFileChanges.None;
    private DateTime lastChangeAt;
    private bool disposed;

    public PhysicalApplicationFileChangeMonitor(string configurationPath, Func<DateTime>? utcNowProvider = null)
    {
        this.configurationPath = Path.GetFullPath(configurationPath);
        this.utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
        pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        projectPaths = new HashSet<string>(pathComparer);
        RebuildWatchers();
    }

    public void WatchProjectFiles(IEnumerable<string> paths)
    {
        lock (gate)
        {
            ThrowIfDisposed();
            projectPaths = paths.Select(Path.GetFullPath).ToHashSet(pathComparer);
            RebuildWatchers();
        }
    }

    public ApplicationFileChanges Poll()
    {
        lock (gate)
        {
            ThrowIfDisposed();
            if (!pending.HasChanges || utcNowProvider() - lastChangeAt < DebounceInterval)
            {
                return ApplicationFileChanges.None;
            }

            var result = pending;
            pending = ApplicationFileChanges.None;
            return result;
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            DisposeWatchers();
        }
    }

    private void RebuildWatchers()
    {
        DisposeWatchers();
        var directories = projectPaths.Append(configurationPath)
            .Select(Path.GetDirectoryName)
            .Where(directory => !string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            .Distinct(pathComparer);

        foreach (var directory in directories)
        {
            var watcher = new FileSystemWatcher(directory!)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;
            watcher.EnableRaisingEvents = true;
            watchers.Add(watcher);
        }
    }

    private void DisposeWatchers()
    {
        foreach (var watcher in watchers)
        {
            watcher.Dispose();
        }

        watchers.Clear();
    }

    private void OnChanged(object sender, FileSystemEventArgs args) => RecordPath(args.FullPath);

    private void OnRenamed(object sender, RenamedEventArgs args)
    {
        RecordPath(args.OldFullPath);
        RecordPath(args.FullPath);
    }

    private void OnError(object sender, ErrorEventArgs args)
    {
        lock (gate)
        {
            if (disposed) return;
            pending = new ApplicationFileChanges(true, true);
            lastChangeAt = utcNowProvider();
        }
    }

    private void RecordPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        lock (gate)
        {
            if (disposed) return;
            var changes = new ApplicationFileChanges(
                pathComparer.Equals(fullPath, configurationPath),
                projectPaths.Contains(fullPath));
            if (!changes.HasChanges) return;
            pending = pending.Merge(changes);
            lastChangeAt = utcNowProvider();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}
