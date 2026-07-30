namespace WolfTodo.Tui.Features.ApplicationShell.Commands;

public sealed record ApplicationCommandTransition(
    ApplicationCommandState State,
    ApplicationCommandOperation Operation = ApplicationCommandOperation.None,
    string? ProjectTitle = null);
