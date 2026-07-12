namespace WolfTodo.Tui.Features.Splash;

public sealed record HomeScreenState(bool IsCommandMode, string Command, string? Error)
{
    public static HomeScreenState Initial { get; } = new(false, string.Empty, null);
}
