namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationCommandState(
    bool IsActive,
    string Value,
    string? Error)
{
    public static ApplicationCommandState Initial { get; } = new(false, string.Empty, null);
}
