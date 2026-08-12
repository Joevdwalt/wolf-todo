using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ActiveTimer(
    TodoIdentity? TodoIdentity,
    string? ProjectTitle,
    string? TodoTitle,
    DateTime StartedAt,
    TimeSpan? Duration = null,
    bool CompletionHandled = false)
{
    public bool IsPomodoro => Duration is not null;

    public bool IsTaskLinked => TodoIdentity is not null && ProjectTitle is not null && TodoTitle is not null;

    public DateTime? EndsAt => Duration is { } duration ? StartedAt + duration : null;

    public TimeSpan Elapsed(DateTime now) => now > StartedAt ? now - StartedAt : TimeSpan.Zero;

    public TimeSpan Remaining(DateTime now)
    {
        if (EndsAt is not { } endsAt || now >= endsAt)
        {
            return TimeSpan.Zero;
        }

        return endsAt - now;
    }

    public bool IsComplete(DateTime now) => EndsAt is { } endsAt && now >= endsAt;

    public DateTime RecordingEnd(DateTime now) => EndsAt is { } endsAt && now > endsAt ? endsAt : now;
}
