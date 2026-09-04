using System.Collections.Immutable;
using Google.Apis.Calendar.v3.Data;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure.Calendar;

public sealed class GoogleCalendarAgendaProvider(
    IGoogleCalendarEventSourceFactory eventSourceFactory,
    GoogleCalendarEventMapper eventMapper) : IPlannerCalendarAgendaProvider
{
    public async Task<PlannerCalendarAgenda> LoadAsync(
        GoogleCalendarConfiguration configuration,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!configuration.Enabled)
        {
            return PlannerCalendarAgenda.Disabled;
        }

        if (configuration.OAuthClientFile is null)
        {
            throw new FileNotFoundException("Google OAuth client file was not found.", configuration.OAuthClientFile);
        }

        await using var eventSource = await eventSourceFactory.CreateAsync(
            configuration.OAuthClientFile,
            cancellationToken);
        var allDay = new List<PlannerCalendarAllDayItem>();
        var meetings = new List<PlannerCalendarMeeting>();
        var unavailableCalendars = new List<string>();

        var primaryEvents = await eventSource.LoadEventsAsync("primary", date, cancellationToken);
        AddAgenda(eventMapper.Map("primary", primaryEvents), allDay, meetings);
        foreach (var calendarId in configuration.AdditionalCalendarIds)
        {
            ImmutableArray<Event> events;
            try
            {
                events = await eventSource.LoadEventsAsync(calendarId, date, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                unavailableCalendars.Add(calendarId);
                continue;
            }

            AddAgenda(eventMapper.Map(calendarId, events), allDay, meetings);
        }

        return new PlannerCalendarAgenda(
            [.. allDay.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)],
            [.. meetings
                .OrderBy(meeting => meeting.Start)
                .ThenBy(meeting => meeting.End)
                .ThenBy(meeting => meeting.Title, StringComparer.OrdinalIgnoreCase)],
            PlannerCalendarSyncState.Ready,
            null,
            unavailableCalendars.Count == 0
                ? null
                : $"Google Calendar unavailable: {string.Join(", ", unavailableCalendars)}.");
    }

    private static void AddAgenda(
        PlannerCalendarAgenda agenda,
        List<PlannerCalendarAllDayItem> allDay,
        List<PlannerCalendarMeeting> meetings)
    {
        allDay.AddRange(agenda.AllDayItems);
        meetings.AddRange(agenda.Meetings);
    }
}
