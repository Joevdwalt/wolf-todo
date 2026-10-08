namespace WolfTodo.Tui.Features.Commands;

public sealed record ApplicationCommandTransition(
    ApplicationCommandState State,
    ApplicationCommandOperation Operation = ApplicationCommandOperation.None,
    string? ProjectTitle = null,
    PomodoroDurationSource? PomodoroDurationSource = null,
    int? PomodoroMinutes = null,
    bool PomodoroUntracked = false,
    string? TaskCode = null);
