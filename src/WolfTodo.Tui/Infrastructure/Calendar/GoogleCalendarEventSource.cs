using System.Collections.Immutable;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace WolfTodo.Tui.Infrastructure.Calendar;

public sealed class GoogleCalendarEventSource : IGoogleCalendarEventSource
{
    private readonly Func<
        string,
        DateTimeOffset,
        DateTimeOffset,
        string?,
        CancellationToken,
        Task<Events>> loadPage;
    private readonly Func<ValueTask> dispose;

    public GoogleCalendarEventSource(CalendarService service)
        : this(
            (calendarId, start, end, pageToken, cancellationToken) =>
                LoadPageAsync(service, calendarId, start, end, pageToken, cancellationToken),
            () =>
            {
                service.Dispose();
                return ValueTask.CompletedTask;
            })
    {
    }

    public GoogleCalendarEventSource(
        Func<string, DateTimeOffset, DateTimeOffset, string?, CancellationToken, Task<Events>> loadPage,
        Func<ValueTask>? dispose = null)
    {
        this.loadPage = loadPage;
        this.dispose = dispose ?? (() => ValueTask.CompletedTask);
    }

    public async Task<ImmutableArray<Event>> LoadEventsAsync(
        string calendarId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        var start = new DateTimeOffset(localStart);
        var end = new DateTimeOffset(localStart.AddDays(1));
        var events = ImmutableArray.CreateBuilder<Event>();
        string? pageToken = null;

        do
        {
            var page = await loadPage(calendarId, start, end, pageToken, cancellationToken);
            events.AddRange(page.Items ?? []);
            pageToken = page.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return events.ToImmutable();
    }

    public ValueTask DisposeAsync() => dispose();

    public static EventsResource.ListRequest CreateRequest(
        CalendarService service,
        string calendarId,
        DateTimeOffset start,
        DateTimeOffset end,
        string? pageToken)
    {
        var request = service.Events.List(calendarId);
        request.TimeMinDateTimeOffset = start;
        request.TimeMaxDateTimeOffset = end;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.ShowDeleted = false;
        request.PageToken = pageToken;
        return request;
    }

    private static Task<Events> LoadPageAsync(
        CalendarService service,
        string calendarId,
        DateTimeOffset start,
        DateTimeOffset end,
        string? pageToken,
        CancellationToken cancellationToken) =>
        CreateRequest(service, calendarId, start, end, pageToken).ExecuteAsync(cancellationToken);
}
