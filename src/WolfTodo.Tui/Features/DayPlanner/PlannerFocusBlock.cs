namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record PlannerFocusBlock(DateTime StartedAt, DateTime EndsAt, string? TodoTitle)
{
    public string Title => string.IsNullOrWhiteSpace(TodoTitle) ? "Pomodoro" : TodoTitle;

    public TimeSpan Remaining(DateTime now) => now >= EndsAt ? TimeSpan.Zero : EndsAt - now;
}
