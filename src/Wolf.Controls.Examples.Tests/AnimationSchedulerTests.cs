using FluentAssertions;
using Wolf.Controls.Examples;

namespace Wolf.Controls.Examples.Tests;

public sealed class AnimationSchedulerTests
{
    [Fact]
    public void NextFrameAt_uses_the_active_spinner_or_progress_deadline()
    {
        var now = DateTimeOffset.UnixEpoch;
        var spinner = GalleryState.Create(now) with { ActiveDemo = DemoId.Spinner };
        var progress = GalleryState.Create(now) with { ActiveDemo = DemoId.ProgressBar };

        AnimationScheduler.NextFrameAt(spinner, now).Should().Be(now + Wolf.Controls.AnimationTiming.FrameInterval);
        AnimationScheduler.NextFrameAt(progress, now).Should().Be(now + Wolf.Controls.AnimationTiming.FrameInterval);
    }
}
