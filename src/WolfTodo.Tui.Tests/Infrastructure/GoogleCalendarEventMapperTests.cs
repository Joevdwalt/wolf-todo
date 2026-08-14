using FluentAssertions;
using Google.Apis.Calendar.v3.Data;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class GoogleCalendarEventMapperTests
{
    private readonly GoogleCalendarEventMapper mapper = new();

    [Fact]
    public void Map_maps_timed_and_all_day_events_with_calendar_metadata()
    {
        var timed = new Event
        {
            Id = "meeting",
            Summary = "Planning",
            Location = "Room 1",
            Description = "Agenda",
            Start = At(9, 0),
            End = At(10, 0),
            Attendees =
            [
                new EventAttendee { DisplayName = "Ada", Email = "ada@example.com", ResponseStatus = "accepted" },
                new EventAttendee { Email = "grace@example.com", ResponseStatus = "tentative" },
                new EventAttendee { Email = "skip@example.com", ResponseStatus = "declined" }
            ]
        };
        var allDay = new Event
        {
            Id = "holiday",
            Summary = "Holiday",
            Start = new EventDateTime { Date = "2026-08-14" },
            End = new EventDateTime { Date = "2026-08-15" }
        };

        var agenda = mapper.Map("team@example.com", [timed, allDay]);

        agenda.Meetings.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PlannerCalendarMeeting("Planning", new TimeOnly(9, 0), new TimeOnly(10, 0))
            {
                EventId = "meeting",
                CalendarId = "team@example.com",
                Location = "Room 1",
                Description = "Agenda",
                Attendees = ["Ada", "grace@example.com"]
            });
        agenda.AllDayItems.Should().ContainSingle().Which.Title.Should().Be("Holiday");
        agenda.AllDayItems[0].CalendarId.Should().Be("team@example.com");
    }

    [Fact]
    public void Map_skips_self_declined_events_and_maps_status_events_as_all_day_items()
    {
        var declined = TimedEvent("Declined", 9, 0, 9, 30);
        declined.Attendees = [new EventAttendee { Self = true, ResponseStatus = "declined" }];
        var focus = TimedEvent("Focus", 10, 0, 11, 0);
        focus.EventType = "focusTime";
        var outOfOffice = TimedEvent("Away", 12, 0, 13, 0);
        outOfOffice.EventType = "outOfOffice";

        var agenda = mapper.Map("primary", [declined, focus, outOfOffice]);

        agenda.Meetings.Should().BeEmpty();
        agenda.AllDayItems.Select(item => item.Kind)
            .Should().Equal(PlannerCalendarItemKind.FocusTime, PlannerCalendarItemKind.OutOfOffice);
    }

    [Fact]
    public void Map_uses_busy_and_thirty_minutes_when_google_data_is_incomplete()
    {
        var calendarEvent = TimedEvent(string.Empty, 15, 0, 14, 0);
        calendarEvent.Summary = " ";
        calendarEvent.Id = null;

        var meeting = mapper.Map("secondary", [calendarEvent]).Meetings.Single();

        meeting.Title.Should().Be("Busy");
        meeting.End.Should().Be(new TimeOnly(15, 30));
        meeting.Identity.Should().StartWith("calendar:secondary:");
    }

    [Fact]
    public void Public_mapping_units_expose_attendee_decline_and_item_kind_decisions()
    {
        var calendarEvent = new Event
        {
            Attendees = [new EventAttendee { Self = true, Email = "me@example.com", ResponseStatus = "declined" }]
        };

        mapper.IsDeclined(calendarEvent).Should().BeTrue();
        mapper.AttendeeNames(calendarEvent).Should().BeEmpty();
        mapper.ItemKind("focusTime").Should().Be(PlannerCalendarItemKind.FocusTime);
        mapper.ItemKind("outOfOffice").Should().Be(PlannerCalendarItemKind.OutOfOffice);
        mapper.ItemKind("default").Should().Be(PlannerCalendarItemKind.Event);
    }

    private static Event TimedEvent(string title, int startHour, int startMinute, int endHour, int endMinute) => new()
    {
        Id = title.ToLowerInvariant(),
        Summary = title,
        Start = At(startHour, startMinute),
        End = At(endHour, endMinute)
    };

    private static EventDateTime At(int hour, int minute) => new()
    {
        DateTimeDateTimeOffset = new DateTimeOffset(
            new DateTime(2026, 8, 14, hour, minute, 0, DateTimeKind.Local))
    };
}
