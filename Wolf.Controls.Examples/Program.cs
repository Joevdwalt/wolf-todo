using Spectre.Console;
using Wolf.Controls;
using Wolf.Controls.Examples;

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    AnsiConsole.Write(GalleryRenderer.Render(GalleryState.Create(DateTimeOffset.UtcNow), DateTimeOffset.UtcNow, GalleryRenderer.SafeWidth()));
    return;
}

var state = GalleryState.Create(DateTimeOffset.UtcNow);
var exitRequested = false;
var cursorVisible = TryGetCursorVisible();

try
{
    SetCursorVisible(false);
    while (!exitRequested)
    {
        var now = DateTimeOffset.UtcNow;
        state = GalleryReducer.Tick(state, now);
        AnsiConsole.Clear();
        AnsiConsole.Write(GalleryRenderer.Render(state, now, GalleryRenderer.SafeWidth()));

        var nextFrame = AnimationScheduler.NextFrameAt(state, now) ?? now + TimeSpan.FromMilliseconds(250);
        while (!Console.KeyAvailable && DateTimeOffset.UtcNow < nextFrame)
        {
            Thread.Sleep(10);
        }

        if (!Console.KeyAvailable)
        {
            continue;
        }

        var transition = GalleryReducer.Reduce(state, Console.ReadKey(intercept: true), DateTimeOffset.UtcNow);
        state = transition.State;
        exitRequested = transition.ExitRequested;
    }
}
finally
{
    if (cursorVisible is { } visible)
    {
        SetCursorVisible(visible);
    }

    AnsiConsole.Clear();
}

static bool? TryGetCursorVisible()
{
    try
    {
#pragma warning disable CA1416
        return Console.CursorVisible;
#pragma warning restore CA1416
    }
    catch (PlatformNotSupportedException)
    {
        return null;
    }
}

static void SetCursorVisible(bool visible)
{
    try
    {
        Console.CursorVisible = visible;
    }
    catch (PlatformNotSupportedException)
    {
        // Some terminals, including macOS Console, do not expose cursor state.
    }
}
