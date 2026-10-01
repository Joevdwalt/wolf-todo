using System.Collections.Immutable;
using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

public sealed class PlannerTimelineViewportTests
{
    [Fact]
    public void WindowPlannerMultiDaySlots_keeps_the_active_slot_visible()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = Enumerable.Range(0, 4)
            .Select(index => new PlannerSlotView(new TimeOnly(6, 0).AddMinutes(index * 15), [], index == 2))
            .ToImmutableArray();
        var columns = new[]
        {
            new PlannerDayColumnView(date, slots, PlannerCalendarAgenda.Disabled, true),
            new PlannerDayColumnView(date.AddDays(1), slots, PlannerCalendarAgenda.Disabled, false)
        };

        var window = new PlannerTimelineViewport().WindowPlannerMultiDaySlots(columns, 2, availableRows: 2);

        window.Should().Contain(2);
        window.Count.Should().BeLessThan(4);
    }

    [Fact]
    public void WindowPlannerMultiDaySlots_does_not_shrink_for_overlapping_items()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(date, includeOverlap: true, itemCount: 6);
        var columns = new[]
        {
            new PlannerDayColumnView(date, slots, PlannerCalendarAgenda.Disabled, true),
            new PlannerDayColumnView(date.AddDays(1), slots, PlannerCalendarAgenda.Disabled, false)
        };

        var window = new PlannerTimelineViewport().WindowPlannerMultiDaySlots(columns, 12, availableRows: 8);

        window.Should().HaveCount(8);
        window.Should().Contain(12);
    }

    [Fact]
    public void WindowPlannerMultiDayTimeline_keeps_now_and_the_selected_slot_together_when_they_fit()
    {
        var today = new DateOnly(2026, 8, 4);
        var slots = Enumerable.Range(0, 4)
            .Select(index => new PlannerSlotView(new TimeOnly(9, 0).AddMinutes(index * 15), [], index == 0))
            .ToImmutableArray();
        var columns = new[] { new PlannerDayColumnView(today, slots, PlannerCalendarAgenda.Disabled, true) };

        var window = new PlannerTimelineViewport().WindowPlannerMultiDayTimeline(
            columns, 0, 4, new DateTime(2026, 8, 4, 9, 30, 0));

        window.SlotIndices.Should().Equal(0, 1, 2);
        window.MarkerSlotIndex.Should().Be(2);
        window.IncludesNow.Should().BeTrue();
    }

    [Fact]
    public void WindowPlannerTimeline_inserts_now_marker_for_selected_today()
    {
        var renderer = new PlannerTimelineViewport();
        var slots = new[]
        {
            new PlannerSlotView(new TimeOnly(9, 0), [], false),
            new PlannerSlotView(new TimeOnly(10, 0), [], true)
        };

        var rows = renderer.WindowPlannerTimeline(
            slots,
            1,
            10,
            new DateOnly(2026, 8, 4),
            new DateTime(2026, 8, 4, 9, 15, 0));

        rows.Should().HaveCount(3);
        rows[0].Should().BeOfType<PlannerSlotTimelineRow>();
        var marker = rows[1].Should().BeOfType<PlannerNowTimelineRow>().Which;
        marker.Time.Should().Be(new TimeOnly(9, 15));
        marker.TimeUntilNextMeeting.Should().BeNull();
        marker.PomodoroRemaining.Should().BeNull();
        rows[2].Should().BeOfType<PlannerSlotTimelineRow>();
    }

    [Fact]
    public void WindowPlannerTimeline_counts_down_to_the_earliest_future_calendar_event()
    {
        var renderer = new PlannerTimelineViewport();
        var meetings = new[]
        {
            new PlannerCalendarMeeting("Active", new TimeOnly(9, 0), new TimeOnly(9, 45)),
            new PlannerCalendarMeeting("Starting now", new TimeOnly(9, 15), new TimeOnly(9, 45)),
            new PlannerCalendarMeeting("Later", new TimeOnly(11, 0), new TimeOnly(11, 30)),
            new PlannerCalendarMeeting("Next solo event", new TimeOnly(10, 30), new TimeOnly(11, 0))
        };

        var rows = renderer.WindowPlannerTimeline(
            [new PlannerSlotView(new TimeOnly(9, 15), [], true)],
            0,
            10,
            new DateOnly(2026, 8, 4),
            new DateTime(2026, 8, 4, 9, 15, 0),
            meetings);

        rows.Should().ContainSingle(row => row is PlannerNowTimelineRow);
        rows.OfType<PlannerNowTimelineRow>().Single().TimeUntilNextMeeting
            .Should().Be(TimeSpan.FromMinutes(75));
        rows.OfType<PlannerNowTimelineRow>().Single().NextMeetingTitle
            .Should().Be("Next solo event");
    }

    [Fact]
    public void WindowPlannerTimeline_adds_active_pomodoro_data_to_the_now_row()
    {
        var renderer = new PlannerTimelineViewport();
        var now = new DateTime(2026, 8, 4, 9, 15, 0);
        var focus = new PlannerFocusBlock(now.AddMinutes(-5), now.AddMinutes(20), "Deep work");

        var rows = renderer.WindowPlannerTimeline(
            [new PlannerSlotView(new TimeOnly(9, 15), [], true)],
            0,
            10,
            new DateOnly(2026, 8, 4),
            now,
            activeFocusBlock: focus);

        var marker = rows.OfType<PlannerNowTimelineRow>().Single();
        marker.PomodoroRemaining.Should().Be(TimeSpan.FromMinutes(20));
        marker.PomodoroTitle.Should().Be("Deep work");
    }

    private static ImmutableArray<PlannerSlotView> MultiDaySlots(
        DateOnly date,
        bool includeOverlap,
        int itemCount = 2)
    {
        var slots = Enumerable.Range(0, DayPlannerPresenter.SlotCount)
            .Select(index => new PlannerSlotView(new TimeOnly(6, 0).AddMinutes(index * 15), [], index == 12))
            .ToArray();
        var overlapItems = Enumerable.Range(0, itemCount).Select(index => new PlannerTimelineItemView(
            PlannerItemType.Task,
            $"task:{index}",
            includeOverlap ? index switch { 0 => "Alpha", 1 => "Beta", int otherIndex => $"T{otherIndex}" } : $"Task {index}",
            new TimeOnly(9, 0),
            new TimeOnly(9, 15),
            PlannerTimeShape.Instant,
            PlannerIntervalState.Instant,
            false,
            index == itemCount - 1,
            IsActive: index == itemCount - 1)).ToImmutableArray();
        slots[12] = slots[12] with { Items = overlapItems };
        return slots.ToImmutableArray();
    }

}
