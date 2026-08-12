namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationCommandTransition(
    ApplicationCommandState State,
    ApplicationCommandOperation Operation = ApplicationCommandOperation.None,
    string? ProjectTitle = null,
    PomodoroDurationSource? PomodoroDurationSource = null,
    int? PomodoroMinutes = null,
    bool PomodoroUntracked = false);

public enum PomodoroDurationSource
{
    ExplicitMinutes,
    SelectedTask
}
