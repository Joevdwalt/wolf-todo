namespace Wolf.Controls.Splash;

/// <summary>Content and start time for an expanding splash panel.</summary>
public sealed record SplashBoxState(string Title, string? Subtitle, DateTimeOffset StartedAt)
{
    public static SplashBoxState Create(string title, string? subtitle, DateTimeOffset now) => new(title, subtitle, now);
}
