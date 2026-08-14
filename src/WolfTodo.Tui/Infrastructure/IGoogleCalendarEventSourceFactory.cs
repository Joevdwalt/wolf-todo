namespace WolfTodo.Tui.Infrastructure;

public interface IGoogleCalendarEventSourceFactory
{
    Task<IGoogleCalendarEventSource> CreateAsync(
        string oauthClientFile,
        CancellationToken cancellationToken);
}
