using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.Timing;

public sealed record PomodoroCompletion(string? TodoTitle, TimeSpan Duration, DateTime CompletedAt)
{
    public string Status => $"{TodoGlyphs.CompletedTodoGlyph} POMODORO COMPLETE · {(int)Duration.TotalMinutes}m" +
        (string.IsNullOrWhiteSpace(TodoTitle) ? string.Empty : $" · {TodoTitle}");

    public string NotificationBody => string.IsNullOrWhiteSpace(TodoTitle)
        ? "Pomodoro complete"
        : $"Pomodoro complete: {TodoTitle}";
}
