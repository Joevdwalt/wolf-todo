namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationLoopEvent(
    ConsoleKeyInfo? Key,
    ApplicationFileChanges FileChanges,
    bool RedrawRequested)
{
    public static ApplicationLoopEvent ForKey(ConsoleKeyInfo key) => new(key, ApplicationFileChanges.None, false);

    public static ApplicationLoopEvent ForFiles(ApplicationFileChanges changes) => new(null, changes, false);

    public static ApplicationLoopEvent ForRedraw() => new(null, ApplicationFileChanges.None, true);
}
