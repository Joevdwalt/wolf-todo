namespace WolfTodo.Tui.Features.Commands;

public sealed record CommandPaletteItem(
    ApplicationActionId Action,
    string Group,
    string Label,
    string Description,
    string Binding,
    bool IsEnabled,
    string? DisabledReason);
