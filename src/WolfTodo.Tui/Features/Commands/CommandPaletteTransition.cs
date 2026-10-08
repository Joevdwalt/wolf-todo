namespace WolfTodo.Tui.Features.Commands;

public sealed record CommandPaletteTransition(
    CommandPaletteState State,
    ApplicationActionId? Action = null);
