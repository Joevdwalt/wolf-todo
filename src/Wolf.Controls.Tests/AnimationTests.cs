using FluentAssertions;
using Wolf.Controls.ProgressBar;

namespace Wolf.Controls.Tests;

public sealed class AnimationTests
{
    [Fact]
    public void Spinner_advances_a_braille_frame_every_frame_interval()
    {
        var start = DateTimeOffset.FromUnixTimeMilliseconds(0);

        Spinner.FrameAt(start).Should().Be("⠋");
        Spinner.FrameAt(start + AnimationTiming.FrameInterval).Should().Be("⠙");
        Spinner.Default.NextFrameAt(new SpinnerState("Loading"), start).Should().Be(start + AnimationTiming.FrameInterval);
    }

    [Fact]
    public void Progress_interpolates_between_previous_and_target_values()
    {
        var start = DateTimeOffset.UnixEpoch;
        var state = new ProgressState("Saving", 0.2, 0.8, start);

        ProgressBar.ProgressBar.ValueAt(state, start).Should().Be(0.2);
        ProgressBar.ProgressBar.ValueAt(state, start + TimeSpan.FromMilliseconds(75)).Should().BeApproximately(0.5, 0.001);
        ProgressBar.ProgressBar.ValueAt(state, start + AnimationTiming.TransitionDuration).Should().Be(0.8);
        ProgressBar.ProgressBar.Default.NextFrameAt(state, start + AnimationTiming.TransitionDuration).Should().BeNull();
    }

    [Fact]
    public void Toast_is_visible_only_within_its_lifetime_and_schedules_transition_frames()
    {
        var start = DateTimeOffset.UnixEpoch;
        var state = new ToastState("Saved", ToastSeverity.Success, start, start + TimeSpan.FromSeconds(3));

        state.IsVisibleAt(start).Should().BeTrue();
        state.IsVisibleAt(state.VisibleUntil).Should().BeFalse();
        Toast.Default.NextFrameAt(state, start).Should().Be(start + AnimationTiming.FrameInterval);
        Toast.Default.NextFrameAt(state, state.VisibleUntil).Should().BeNull();
    }
}
