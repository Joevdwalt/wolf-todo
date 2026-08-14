using FluentAssertions;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class PlannerCalendarMeetingTests
{
    [Fact]
    public void Identity_remains_source_safe_with_and_without_an_event_id()
    {
        var primary = new PlannerCalendarMeeting("Planning", new TimeOnly(9, 0), new TimeOnly(9, 30))
        {
            EventId = "shared-event",
            CalendarId = "primary"
        };
        var secondary = primary with { CalendarId = "team@example.com" };
        var fallback = primary with { EventId = null };

        primary.Identity.Should().Be("calendar:primary:shared-event");
        secondary.Identity.Should().Be("calendar:team@example.com:shared-event");
        fallback.Identity.Should().StartWith("calendar:primary:");
        secondary.Identity.Should().NotBe(primary.Identity);
    }
}
