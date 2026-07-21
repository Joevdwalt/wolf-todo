using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure;

public sealed class GoogleCalendarAgendaProvider(string tokenDirectory) : IPlannerCalendarAgendaProvider
{
    private static readonly string[] Scopes = [CalendarService.Scope.CalendarEventsReadonly];

    public async Task<PlannerCalendarAgenda> LoadAsync(
        GoogleCalendarConfiguration configuration,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!configuration.Enabled)
        {
            return PlannerCalendarAgenda.Disabled;
        }

        if (configuration.OAuthClientFile is null || !File.Exists(configuration.OAuthClientFile))
        {
            throw new FileNotFoundException("Google OAuth client file was not found.", configuration.OAuthClientFile);
        }

        await using var clientFile = File.OpenRead(configuration.OAuthClientFile);
        var secrets = GoogleClientSecrets.FromStream(clientFile).Secrets;
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            "wtodo",
            cancellationToken,
            new FileDataStore(tokenDirectory, true));
        using var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Wolf Todo"
        });

        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        var localEnd = localStart.AddDays(1);
        var request = service.Events.List("primary");
        request.TimeMinDateTimeOffset = new DateTimeOffset(localStart);
        request.TimeMaxDateTimeOffset = new DateTimeOffset(localEnd);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.ShowDeleted = false;
        var response = await request.ExecuteAsync(cancellationToken);
        var allDay = new List<PlannerCalendarAllDayItem>();
        var meetings = new List<PlannerCalendarMeeting>();

        foreach (var calendarEvent in response.Items ?? [])
        {
            if (IsDeclined(calendarEvent))
            {
                continue;
            }

            var title = string.IsNullOrWhiteSpace(calendarEvent.Summary) ? "Busy" : calendarEvent.Summary;
            var kind = ItemKind(calendarEvent.EventType);
            if (calendarEvent.Start?.DateTimeDateTimeOffset is null || kind != PlannerCalendarItemKind.Event)
            {
                allDay.Add(new PlannerCalendarAllDayItem(title, kind));
                continue;
            }

            var start = TimeOnly.FromDateTime(calendarEvent.Start.DateTimeDateTimeOffset.Value.LocalDateTime);
            var end = calendarEvent.End?.DateTimeDateTimeOffset is { } endDateTime
                ? TimeOnly.FromDateTime(endDateTime.LocalDateTime)
                : start.AddMinutes(30);
            if (end <= start)
            {
                end = start.AddMinutes(30);
            }

            var attendees = (calendarEvent.Attendees ?? [])
                .Where(attendee => attendee.ResponseStatus != "declined")
                .Select(attendee => string.IsNullOrWhiteSpace(attendee.DisplayName)
                    ? attendee.Email
                    : attendee.DisplayName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => name!)
                .ToArray();
            meetings.Add(new PlannerCalendarMeeting(title, start, end)
            {
                EventId = calendarEvent.Id,
                Location = calendarEvent.Location,
                Attendees = [.. attendees],
                Description = calendarEvent.Description
            });
        }

        return new PlannerCalendarAgenda([.. allDay], [.. meetings], PlannerCalendarSyncState.Ready);
    }

    private static bool IsDeclined(Event calendarEvent) =>
        calendarEvent.Attendees?.Any(attendee => attendee.Self == true && attendee.ResponseStatus == "declined") == true;

    private static PlannerCalendarItemKind ItemKind(string? eventType) => eventType switch
    {
        "focusTime" => PlannerCalendarItemKind.FocusTime,
        "outOfOffice" => PlannerCalendarItemKind.OutOfOffice,
        _ => PlannerCalendarItemKind.Event
    };
}
