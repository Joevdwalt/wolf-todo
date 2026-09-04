using System.Collections.Immutable;
using Google.Apis.Calendar.v3.Data;

namespace WolfTodo.Tui.Infrastructure.Calendar;

public interface IGoogleCalendarEventSource : IAsyncDisposable
{
    Task<ImmutableArray<Event>> LoadEventsAsync(
        string calendarId,
        DateOnly date,
        CancellationToken cancellationToken);
}
