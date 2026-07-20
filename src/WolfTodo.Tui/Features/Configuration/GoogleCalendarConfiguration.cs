namespace WolfTodo.Tui.Features.Configuration;

public sealed record GoogleCalendarConfiguration(bool Enabled, string? OAuthClientFile)
{
    public static GoogleCalendarConfiguration Disabled { get; } = new(false, null);
}
