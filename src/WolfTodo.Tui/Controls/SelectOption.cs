namespace WolfTodo.Tui.Controls;

public sealed record SelectOption(
    string Label,
    string? Detail = null,
    bool IsEnabled = true);
