using FluentAssertions;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class CalendarItemRendererTests
{
    private readonly CalendarItemRenderer renderer = new();

    [Fact]
    public void MeetingLabel_includes_time_range_and_title()
    {
        var meeting = new PlannerCalendarMeeting("Planning", new TimeOnly(9, 0), new TimeOnly(9, 45));

        renderer.MeetingLabel(meeting).Should().Be("09:00–09:45 Planning");
    }

    [Fact]
    public void MeetingTimeAndDuration_includes_minutes()
    {
        var meeting = new PlannerCalendarMeeting("Planning", new TimeOnly(9, 0), new TimeOnly(9, 45));

        renderer.MeetingTimeAndDuration(meeting).Should().Be("09:00–09:45 · 45m");
    }

    [Fact]
    public void MeetingDescriptionPreview_normalizes_and_truncates_long_descriptions()
    {
        var description = "alpha\n  beta\tgamma " + new string('x', 130);

        var preview = renderer.MeetingDescriptionPreview(description);

        preview.Should().StartWith("alpha beta gamma");
        preview.Should().HaveLength(118);
        preview.Should().EndWith("…");
    }

    [Fact]
    public void AllDayKindLabel_maps_calendar_item_kinds()
    {
        renderer.AllDayKindLabel(PlannerCalendarItemKind.FocusTime).Should().Be("Focus time");
        renderer.AllDayKindLabel(PlannerCalendarItemKind.OutOfOffice).Should().Be("Out of office");
        renderer.AllDayKindLabel(PlannerCalendarItemKind.Todo).Should().Be("Todo");
        renderer.AllDayKindLabel(PlannerCalendarItemKind.Event).Should().Be("Calendar event");
    }
}
