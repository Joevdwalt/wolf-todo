using FluentAssertions;
using Wolf.Controls.Splash;

namespace Wolf.Controls.Tests;

public sealed class SplashBoxTests
{
    [Fact]
    public void ExpansionAt_eases_from_zero_to_one_over_eight_hundred_milliseconds()
    {
        var start = DateTimeOffset.UnixEpoch;
        var state = SplashBoxState.Create("Wolf Controls", "Ready", start);

        SplashBox.ExpansionAt(state, start).Should().Be(0);
        SplashBox.ExpansionAt(state, start + TimeSpan.FromMilliseconds(400)).Should().BeGreaterThan(0.5).And.BeLessThan(1);
        SplashBox.ExpansionAt(state, start + SplashBox.ExpansionDuration).Should().Be(1);
    }

    [Fact]
    public void SizeAt_grows_from_the_small_panel_to_the_available_terminal_area()
    {
        var start = DateTimeOffset.UnixEpoch;
        var state = SplashBoxState.Create("Wolf Controls", "Ready", start);
        var constraints = new ControlConstraints(80, 24);

        SplashBox.SizeAt(state, constraints, start).Should().Be(new SplashBoxSize(12, 3));
        SplashBox.SizeAt(state, constraints, start + SplashBox.ExpansionDuration).Should().Be(new SplashBoxSize(80, 24));
    }

    [Fact]
    public void NextFrameAt_stops_after_expansion_but_the_final_panel_can_still_render()
    {
        var start = DateTimeOffset.UnixEpoch;
        var state = SplashBoxState.Create("Wolf Controls", "Ready", start);

        SplashBox.Default.NextFrameAt(state, start).Should().Be(start + SplashBox.FrameInterval);
        SplashBox.Default.NextFrameAt(state, start + SplashBox.ExpansionDuration).Should().BeNull();
        SplashBox.Default.Render(state, ControlTheme.Default, new ControlConstraints(80, 24), start + SplashBox.ExpansionDuration)
            .Should().NotBeNull();
    }

    [Fact]
    public void ContentVisibleAt_reveals_content_only_after_expansion_completes()
    {
        var start = DateTimeOffset.UnixEpoch;
        var state = SplashBoxState.Create("Wolf Controls", "Ready", start);

        SplashBox.ContentVisibleAt(state, start).Should().BeFalse();
        SplashBox.ContentVisibleAt(state, start + SplashBox.ExpansionDuration - TimeSpan.FromMilliseconds(1)).Should().BeFalse();
        SplashBox.ContentVisibleAt(state, start + SplashBox.ExpansionDuration).Should().BeTrue();
    }
}
