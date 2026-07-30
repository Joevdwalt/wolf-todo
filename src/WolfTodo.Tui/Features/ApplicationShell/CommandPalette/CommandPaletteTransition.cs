using WolfTodo.Tui.Features.ApplicationShell.Actions;

namespace WolfTodo.Tui.Features.ApplicationShell.CommandPalette;

public sealed record CommandPaletteTransition(
    CommandPaletteState State,
    ApplicationActionId? Action = null);
