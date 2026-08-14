using Google.Apis.Calendar.v3.Data;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure;

public sealed class GoogleCalendarEventMapper
{
    public PlannerCalendarAgenda Map(string calendarId, IEnumerable<Event> events)
    {
        var allDay = new List<PlannerCalendarAllDayItem>();
        var meetings = new List<PlannerCalendarMeeting>();

        foreach (var calendarEvent in events)
        {
            if (IsDeclined(calendarEvent))
            {
                continue;
            }

            var title = string.IsNullOrWhiteSpace(calendarEvent.Summary) ? "Busy" : calendarEvent.Summary;
            var kind = ItemKind(calendarEvent.EventType);
            var attendees = AttendeeNames(calendarEvent);
            if (calendarEvent.Start?.DateTimeDateTimeOffset is null || kind != PlannerCalendarItemKind.Event)
            {
                allDay.Add(new PlannerCalendarAllDayItem(title, kind)
                {
                    EventId = calendarEvent.Id,
                    CalendarId = calendarId,
                    Location = calendarEvent.Location,
                    Attendees = [.. attendees],
                    Description = calendarEvent.Description
                });
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

            meetings.Add(new PlannerCalendarMeeting(title, start, end)
            {
                EventId = calendarEvent.Id,
                CalendarId = calendarId,
                Location = calendarEvent.Location,
                Attendees = [.. attendees],
                Description = calendarEvent.Description
            });
        }

        return new PlannerCalendarAgenda([.. allDay], [.. meetings], PlannerCalendarSyncState.Ready);
    }

    public string[] AttendeeNames(Event calendarEvent) =>
        [.. (calendarEvent.Attendees ?? [])
            .Where(attendee => attendee.ResponseStatus != "declined")
            .Select(attendee => string.IsNullOrWhiteSpace(attendee.DisplayName)
                ? attendee.Email
                : attendee.DisplayName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => name!)];

    public bool IsDeclined(Event calendarEvent) =>
        calendarEvent.Attendees?.Any(
            attendee => attendee.Self == true && attendee.ResponseStatus == "declined") == true;

    public PlannerCalendarItemKind ItemKind(string? eventType) => eventType switch
    {
        "focusTime" => PlannerCalendarItemKind.FocusTime,
        "outOfOffice" => PlannerCalendarItemKind.OutOfOffice,
        _ => PlannerCalendarItemKind.Event
    };
}
