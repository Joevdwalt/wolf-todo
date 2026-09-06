namespace Wolf.Controls;

/// <summary>Shared timing constants for host-driven control animations.</summary>
public static class AnimationTiming
{
    public static TimeSpan FrameInterval { get; } = TimeSpan.FromMilliseconds(80);

    public static TimeSpan TransitionDuration { get; } = TimeSpan.FromMilliseconds(150);
}
