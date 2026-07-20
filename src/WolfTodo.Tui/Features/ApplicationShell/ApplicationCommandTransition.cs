namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationCommandTransition(
    ApplicationCommandState State,
    ApplicationCommandOperation Operation = ApplicationCommandOperation.None,
    string? ProjectTitle = null);
