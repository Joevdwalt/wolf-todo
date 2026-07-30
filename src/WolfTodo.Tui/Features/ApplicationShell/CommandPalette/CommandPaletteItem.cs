using WolfTodo.Tui.Features.ApplicationShell.Actions;

namespace WolfTodo.Tui.Features.ApplicationShell.CommandPalette;

public sealed record CommandPaletteItem(
    ApplicationActionId Action,
    string Group,
    string Label,
    string Description,
    string Binding,
    bool IsEnabled,
    string? DisabledReason);
