namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationLoopWaiter(
    ITerminalUi terminalUi,
    IApplicationFileChangeMonitor fileChangeMonitor,
    Func<DateTime>? utcNowProvider = null)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private (int Width, int Height)? lastDimensions = terminalUi.GetDimensions() is { Width: > 0, Height: > 0 } size ? size : null;
    private readonly Func<DateTime> utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);

    public ApplicationLoopEvent Wait(TimeSpan? redrawInterval)
    {
        var deadline = redrawInterval is null ? (DateTime?)null : utcNowProvider() + redrawInterval.Value;

        while (true)
        {
            var changes = fileChangeMonitor.Poll();
            if (changes.HasChanges)
            {
                return ApplicationLoopEvent.ForFiles(changes);
            }

            var dimensions = terminalUi.GetDimensions();
            if (dimensions is { Width: > 0, Height: > 0 })
            {
                if (lastDimensions is not null && lastDimensions != dimensions)
                {
                    lastDimensions = dimensions;
                    return ApplicationLoopEvent.ForRedraw();
                }
                lastDimensions = dimensions;
            }

            var wait = deadline is null
                ? PollInterval
                : deadline.Value - utcNowProvider() < PollInterval
                    ? deadline.Value - utcNowProvider()
                    : PollInterval;
            if (wait <= TimeSpan.Zero)
            {
                return ApplicationLoopEvent.ForRedraw();
            }

            var key = terminalUi.ReadKey(wait);
            if (key is not null)
            {
                return ApplicationLoopEvent.ForKey(key.Value);
            }

            if (deadline is not null && utcNowProvider() >= deadline.Value)
            {
                return ApplicationLoopEvent.ForRedraw();
            }
        }
    }
}
