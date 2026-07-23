namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationCommandState(
    bool IsActive,
    string Value,
    string? Error)
{
    public string? CompletionSeed { get; init; }

    public int CompletionIndex { get; init; } = -1;

    public static ApplicationCommandState Initial { get; } = new(false, string.Empty, null);
}
