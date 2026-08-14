namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record PomodoroCompletion(string? TodoTitle, TimeSpan Duration, DateTime CompletedAt)
{
    public string Status => $"✓ POMODORO COMPLETE · {(int)Duration.TotalMinutes}m" +
        (string.IsNullOrWhiteSpace(TodoTitle) ? string.Empty : $" · {TodoTitle}");

    public string NotificationBody => string.IsNullOrWhiteSpace(TodoTitle)
        ? "Pomodoro complete"
        : $"Pomodoro complete: {TodoTitle}";
}
