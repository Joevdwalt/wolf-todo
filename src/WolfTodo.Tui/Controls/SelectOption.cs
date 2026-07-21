namespace WolfTodo.Tui.Controls;

internal sealed record SelectOption(
    string Label,
    string? Detail = null,
    bool IsEnabled = true);
