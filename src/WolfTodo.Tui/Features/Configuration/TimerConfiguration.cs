namespace WolfTodo.Tui.Features.Configuration;

public sealed record TimerConfiguration(
    string NotesDirectory,
    int PomodoroMinutes = 25,
    bool Bell = true)
{
    public TimeSpan PomodoroDuration => TimeSpan.FromMinutes(PomodoroMinutes);

    public static TimerConfiguration? Disabled => null;
}
