namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record CommandPaletteState(
    bool IsOpen,
    bool IsSearching,
    string Query,
    int SelectedIndex,
    string? Error)
{
    public static CommandPaletteState Closed { get; } = new(false, false, string.Empty, 0, null);
}
