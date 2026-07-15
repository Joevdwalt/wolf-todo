namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record CommandPaletteTransition(
    CommandPaletteState State,
    ApplicationActionId? Action = null);
