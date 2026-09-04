namespace WolfTodo.Tui.Infrastructure.Calendar;

public interface IGoogleCalendarEventSourceFactory
{
    Task<IGoogleCalendarEventSource> CreateAsync(
        string oauthClientFile,
        CancellationToken cancellationToken);
}
