using Wolf.Controls;

namespace Wolf.Controls.Examples;

internal static class AnimationScheduler
{
    public static DateTimeOffset? NextFrameAt(GalleryState state, DateTimeOffset now)
    {
        var frames = new List<DateTimeOffset>();
        if (state.ActiveDemo == DemoId.Spinner)
        {
            frames.Add(Spinner.Default.NextFrameAt(new SpinnerState("Loading controls"), now)!.Value);
        }

        if (state.ActiveDemo == DemoId.ProgressBar)
        {
            frames.Add(now + AnimationTiming.FrameInterval);
        }

        if (state.Toast is not null && Toast.Default.NextFrameAt(state.Toast, now) is { } toastFrame)
        {
            frames.Add(toastFrame);
        }

        return frames.Count == 0 ? null : frames.Min();
    }
}
