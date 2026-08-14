using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record GoogleCalendarConfiguration(bool Enabled, string? OAuthClientFile)
{
    public ImmutableArray<string> AdditionalCalendarIds { get; init; } = [];

    public static GoogleCalendarConfiguration Disabled { get; } = new(false, null);
}
