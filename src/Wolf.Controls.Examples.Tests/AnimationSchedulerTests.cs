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

    [Fact]
    public void NextFrameAt_schedules_splash_expansion_frames_until_completion()
    {
        var now = DateTimeOffset.UnixEpoch;
        var splash = GalleryState.Create(now) with { ActiveDemo = DemoId.Splash };

        AnimationScheduler.NextFrameAt(splash, now).Should().Be(now + Wolf.Controls.Splash.SplashBox.FrameInterval);
        AnimationScheduler.NextFrameAt(splash, now + Wolf.Controls.Splash.SplashBox.ExpansionDuration).Should().BeNull();
    }
}
