using System.Collections.Immutable;
using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

public sealed class PlannerMultiDayTimelineRendererTests
{
    [Fact]
    public void CreatePlannerMultiDayTimelineTable_places_now_in_todays_date_column()
    {
        var today = new DateOnly(2026, 8, 4);
        var slots = new[] { new PlannerSlotView(new TimeOnly(9, 0), [], true) };
        var columns = new[]
        {
            new PlannerDayColumnView(today, [.. slots], PlannerCalendarAgenda.Disabled, true),
            new PlannerDayColumnView(today.AddDays(1), [.. slots], PlannerCalendarAgenda.Disabled, false)
        };
        var lineRenderer = new PlannerTimelineLineRenderer();
        var viewport = new PlannerTimelineViewport();
        var renderer = new PlannerMultiDayTimelineRenderer(
            () => 80, () => new DateTime(2026, 8, 4, 9, 1, 0),
            lineRenderer, viewport, new PlannerMultiDayCellRenderer(lineRenderer));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(
            columns, 0, 2, TuiThemes.Wolf), 80);

        output.Should().Contain("TUE 04").And.Contain("WED 05").And.Contain("NOW");
        output.Split(Environment.NewLine).Count(line => line.Contains("NOW")).Should().Be(1);
    }

    [Fact]
    public void MultiDay_table_fits_two_24_cell_date_columns_at_80_cells_and_renders_now_only_today()
    {
        var today = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(today, includeOverlap: true);
        var columns = new[]
        {
            new PlannerDayColumnView(today, slots, PlannerCalendarAgenda.Disabled, true),
            new PlannerDayColumnView(today.AddDays(1), slots, PlannerCalendarAgenda.Disabled, false)
        };
        var view = MultiDayView(today, columns, selectedSlot: 12);
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 4, 9, 15, 0));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 30, TuiThemes.Wolf), 80);
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Max(line => line.Length).Should().BeLessThanOrEqualTo(80);
        output.Should().Contain("▶ TUE 04")
            .And.Contain("WED 05")
            .And.Contain("NOW");
        lines.Should().Contain(line => line.Contains("Alpha", StringComparison.Ordinal) &&
                                       line.Contains("Beta", StringComparison.Ordinal),
            "overlapping items share one physical row as horizontal segments");
        lines.Count(line => line.Contains("NOW", StringComparison.Ordinal)).Should().Be(1);
    }

    [Fact]
    public void MultiDay_table_fits_three_date_columns_on_a_wide_terminal()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(date, includeOverlap: true);
        var columns = Enumerable.Range(0, 3)
            .Select(offset => new PlannerDayColumnView(
                date.AddDays(offset),
                slots,
                PlannerCalendarAgenda.Disabled,
                offset == 1))
            .ToArray();
        var view = MultiDayView(date.AddDays(1), columns, selectedSlot: 12);
        var renderer = CreateRenderer(100, nowProvider: () => new DateTime(2026, 8, 8, 9, 15, 0));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 20, TuiThemes.Wolf), 100);
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Max(line => line.GetCellWidth()).Should().BeLessThanOrEqualTo(100);
        output.Should().Contain("TUE 04").And.Contain("WED 05").And.Contain("THU 06");
    }

    [Fact]
    public void MultiDay_overlaps_pack_at_the_left_when_the_date_pane_has_spare_width()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(date, includeOverlap: true, itemCount: 3);
        var view = MultiDayView(date,
            [new PlannerDayColumnView(date, slots, PlannerCalendarAgenda.Disabled, true)],
            selectedSlot: 12);
        var renderer = CreateRenderer(100, nowProvider: () => new DateTime(2026, 8, 8, 9, 15, 0));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 20, TuiThemes.Wolf), 100);
        var row = output.Split(Environment.NewLine).Single(line => line.Contains("Alpha") && line.Contains("Beta"));
        var firstSeparator = row.IndexOf('┊');
        var secondSeparator = row.IndexOf('┊', firstSeparator + 1);

        row.Should().Contain("┊  ○ Beta").And.Contain("┊ ▶ ○ T2");
        firstSeparator.Should().BeLessThan(row.IndexOf("Alpha") + 10);
        secondSeparator.Should().BeLessThan(row.IndexOf("Beta") + 10);
        row.LastIndexOf('│').Should().BeGreaterThan(row.IndexOf("T2") + 30,
            "unused pane width belongs after the last item");
    }

    [Fact]
    public void MultiDay_table_keeps_now_visible_with_a_distant_selection_when_both_fit()
    {
        var today = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(today, includeOverlap: false);
        var columns = new[]
        {
            new PlannerDayColumnView(today, slots, PlannerCalendarAgenda.Disabled, true)
        };
        var view = MultiDayView(today, columns, selectedSlot: 0);
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 4, 8, 0, 0));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 10, TuiThemes.Wolf), 80);

        output.Should().Contain("06:00").And.Contain("NOW");
    }

    [Fact]
    public void MultiDay_table_keeps_to_one_timeline_row_when_only_one_is_available()
    {
        var today = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(today, includeOverlap: false);
        var view = MultiDayView(today,
        [new PlannerDayColumnView(today, slots, PlannerCalendarAgenda.Disabled, true)],
            selectedSlot: 12);
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 4, 9, 0, 0));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 1, TuiThemes.Wolf), 80);

        output.Should().NotContain("NOW");
        output.Split(Environment.NewLine).Should().ContainSingle(line => line.Contains("09:00"));
    }

    [Fact]
    public void MultiDay_overflow_window_keeps_selected_item_visible_and_reports_hidden_count()
    {
        var today = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(today, includeOverlap: true, itemCount: 6);
        var columns = new[]
        {
            new PlannerDayColumnView(today, slots, PlannerCalendarAgenda.Disabled, true),
            new PlannerDayColumnView(today.AddDays(1), slots, PlannerCalendarAgenda.Disabled, false)
        };
        var view = MultiDayView(today, columns, selectedSlot: 12);
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 6, 9, 15, 0));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 30, TuiThemes.Wolf), 80);

        output.Should().Contain("T5").And.Contain("+4");
        output.Should().NotContain("T1").And.NotContain("T2");
        output.Split(Environment.NewLine).Should().NotContain(line => line.Trim() == "5",
            "multiday segments truncate instead of wrapping");
    }

    [Fact]
    public void MultiDay_finish_cues_follow_overlapping_task_and_between_tick_calendar_event()
    {
        var date = new DateOnly(2026, 8, 4);
        var todo = Todo("Write brief") with
        {
            Schedule = new TodoSchedule(date, new TimeOnly(9, 30)),
            Duration = TimeSpan.FromMinutes(60)
        };
        var agenda = new PlannerCalendarAgenda([], [
            new PlannerCalendarMeeting("Review", new TimeOnly(9, 40), new TimeOnly(10, 10))
        ], PlannerCalendarSyncState.Ready);
        var presented = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/fixtures/work.md", [todo])], []),
            PlannerState.CreateInitial(date) with { SlotIndex = 14 }, agenda);
        var view = presented with
        {
            State = presented.State with { ViewMode = PlannerViewMode.MultiDay },
            DayColumns = [new PlannerDayColumnView(date, presented.Slots, agenda, true)]
        };
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);
        var lines = output.Split(Environment.NewLine);

        lines.Single(line => line.Contains("│ 10:00") && line.Contains("→│10:10"))
            .Should().Contain("│").And.Contain("→│10:10");
        lines.Single(line => line.Contains("→│10:30"))
            .Should().Contain("→│10:30");
        Render(new PlannerDetailRenderer().PlannerCompactDetail(view, TuiThemes.Wolf), 80)
            .Should().Contain("09:30–10:30 · 60m · Write brief (todo)");
        lines.Should().OnlyContain(line => line.GetCellWidth() <= 80);
    }

    [Fact]
    public void MultiDay_shows_simultaneous_finishes_and_one_slot_finish_without_extra_rows()
    {
        var date = new DateOnly(2026, 8, 4);
        var tasks = new[]
        {
            Todo("Draft") with { Schedule = new TodoSchedule(date, new TimeOnly(9, 30)),
                Duration = TimeSpan.FromMinutes(45) },
            Todo("Review") with { SourceLine = 2, Schedule = new TodoSchedule(date, new TimeOnly(9, 30)),
                Duration = TimeSpan.FromMinutes(45) },
            Todo("Quick call") with { SourceLine = 3, Schedule = new TodoSchedule(date, new TimeOnly(11, 0)),
                Duration = TimeSpan.FromMinutes(15) }
        };
        var presented = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/fixtures/work.md", tasks.ToImmutableArray())], []),
            PlannerState.CreateInitial(date) with { SlotIndex = 14 });
        var view = presented with
        {
            State = presented.State with { ViewMode = PlannerViewMode.MultiDay },
            DayColumns = [new PlannerDayColumnView(date, presented.Slots, presented.CalendarAgenda, true)]
        };
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);
        var lines = output.Split(Environment.NewLine);
        var endingRow = lines.Single(line => line.Contains("│ 10:00") && line.Contains("→│10:15"));

        endingRow.Split("→│10:15").Length.Should().Be(3);
        var overlapStart = lines.Single(line => line.Contains("│ 09:30") && line.Contains("Draft") && line.Contains("Review"));
        var startSegments = overlapStart.Split('┊');
        var endSegments = endingRow.Split('┊');
        startSegments[1].Should().Contain("  ○ Review");
        endSegments[1].Should().Contain(" →│10:15",
            "the finish marker keeps the exact spacing used by the second item lane");

        lines.Single(line => line.Contains("│ 11:00") && line.Contains("Quick call"))
            .Should().Contain("→│11:15");
        lines.Count(line => line.Contains("│ 10:00")).Should().Be(1);
    }

    [Fact]
    public void MultiDay_keeps_later_overlaps_in_their_lanes_after_earlier_items_finish()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = Enumerable.Range(0, DayPlannerPresenter.SlotCount)
            .Select(index => new PlannerSlotView(new TimeOnly(6, 0).AddMinutes(index * 15), [], false))
            .ToArray();
        PlannerTimelineItemView Item(string identity, string title, TimeOnly start, TimeOnly end) =>
            new(PlannerItemType.Task, identity, title, start, end,
                PlannerTimeShape.Duration, PlannerIntervalState.Start, false, false);
        var recovery = Item("recovery", "Forward money Recovery", new TimeOnly(6, 15), new TimeOnly(6, 45));
        var contract = Item("contract", "Francois Contract update", new TimeOnly(6, 15), new TimeOnly(6, 45));
        var agentathon = Item("agentathon", "Agentathon", new TimeOnly(6, 30), new TimeOnly(7, 0));
        var followup = Item("followup", "Followup with frans on quote", new TimeOnly(6, 30), new TimeOnly(7, 0));
        slots[1] = slots[1] with { Items = [recovery, contract] };
        slots[2] = slots[2] with
        {
            Items = [recovery with { IntervalState = PlannerIntervalState.End },
                contract with { IntervalState = PlannerIntervalState.End },
                agentathon with { IsSelected = true }, followup]
        };
        slots[3] = slots[3] with
        {
            Items = [agentathon with { IntervalState = PlannerIntervalState.End },
                followup with { IntervalState = PlannerIntervalState.End }]
        };
        var column = new PlannerDayColumnView(date, [.. slots], new PlannerCalendarAgenda([], [], PlannerCalendarSyncState.Ready), true);
        var view = MultiDayView(date, [column], selectedSlot: 2);
        var renderer = CreateRenderer(150, () => new DateTime(2026, 8, 6));

        var lines = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 12, TuiThemes.Wolf), 150)
            .Split(Environment.NewLine);
        var start = lines.Single(line => line.Contains("│ 06:30") && line.Contains("Agentathon"));
        var startIndex = Array.IndexOf(lines, start);
        var finish = lines[startIndex + 1];
        var agentGlyph = start.LastIndexOf('○', start.IndexOf("Agentathon", StringComparison.Ordinal));
        var followupGlyph = start.LastIndexOf('○', start.IndexOf("Followup with frans", StringComparison.Ordinal));
        var firstFinish = finish.IndexOf("→│07:00", StringComparison.Ordinal);
        var secondFinish = finish.LastIndexOf("→│07:00", StringComparison.Ordinal);

        finish.Should().Contain("  │").And.Contain("┊");
        firstFinish.Should().BeGreaterThan(0);
        secondFinish.Should().BeGreaterThan(firstFinish);
        (firstFinish + 1).Should().Be(agentGlyph);
        (secondFinish + 1).Should().Be(followupGlyph);
        lines.Should().OnlyContain(line => line.GetCellWidth() <= 150);
    }

    [Fact]
    public void MultiDay_keeps_overnight_task_on_its_date_without_a_false_finish_cue()
    {
        var date = new DateOnly(2026, 8, 4);
        var todo = Todo("Late work") with
        {
            Schedule = new TodoSchedule(date, new TimeOnly(21, 45)),
            Duration = TimeSpan.FromHours(4)
        };
        var presented = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/fixtures/work.md", [todo])], []),
            PlannerState.CreateInitial(date) with { SlotIndex = 63 });
        var view = presented with
        {
            State = presented.State with { ViewMode = PlannerViewMode.MultiDay },
            DayColumns = [new PlannerDayColumnView(date, presented.Slots, presented.CalendarAgenda, true)]
        };
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);

        output.Split(Environment.NewLine).Single(line => line.Contains("Late work"))
            .Should().Contain("Late work").And.NotContain("→│");
        Render(new PlannerDetailRenderer().PlannerCompactDetail(view, TuiThemes.Wolf), 80)
            .Should().Contain("21:45–WED 05 01:45 · 240m");
    }

    [Fact]
    public void MultiDay_finish_cues_align_after_titles_of_different_lengths()
    {
        var date = new DateOnly(2026, 8, 4);
        var tasks = new[]
        {
            Todo("Short") with { Schedule = new TodoSchedule(date, new TimeOnly(9, 0)),
                Duration = TimeSpan.FromMinutes(15) },
            Todo("A much longer task title") with { SourceLine = 2,
                Schedule = new TodoSchedule(date, new TimeOnly(10, 0)), Duration = TimeSpan.FromMinutes(15) }
        };
        var presented = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/fixtures/work.md", tasks.ToImmutableArray())], []),
            PlannerState.CreateInitial(date));
        var view = presented with
        {
            State = presented.State with { ViewMode = PlannerViewMode.MultiDay },
            DayColumns = [new PlannerDayColumnView(date, presented.Slots, presented.CalendarAgenda, true)]
        };
        var output = Render(CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 6))
            .CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);
        var shortLine = output.Split(Environment.NewLine).Single(line => line.Contains("Short") && line.Contains("→│09:15"));
        var longLine = output.Split(Environment.NewLine).Single(line => line.Contains("longer task title") && line.Contains("→│10:15"));

        shortLine.IndexOf("→│09:15", StringComparison.Ordinal)
            .Should().Be(longLine.IndexOf("→│10:15", StringComparison.Ordinal));
    }

    [Fact]
    public void MultiDay_pomodoro_finish_uses_actual_time_and_instant_task_has_no_cue()
    {
        var date = new DateOnly(2026, 8, 4);
        var focus = new PlannerFocusBlock(date.ToDateTime(new TimeOnly(9, 7)),
            date.ToDateTime(new TimeOnly(9, 52)), "Focus");
        var instant = Todo("Send email") with
        {
            Schedule = new TodoSchedule(date, new TimeOnly(11, 0))
        };
        var presented = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/fixtures/work.md", [instant])], []),
            PlannerState.CreateInitial(date) with { SlotIndex = 14 }, activeFocusBlock: focus);
        var view = presented with
        {
            State = presented.State with { ViewMode = PlannerViewMode.MultiDay },
            DayColumns = [new PlannerDayColumnView(date, presented.Slots, presented.CalendarAgenda, true)]
        };
        var renderer = CreateRenderer(80, nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);

        output.Should().Contain("→│09:52");
        output.Split(Environment.NewLine).Single(line => line.Contains("Send email"))
            .Should().NotContain("→│");
        Render(new PlannerDetailRenderer().PlannerCompactDetail(view, TuiThemes.Wolf), 80)
            .Should().Contain("09:07–09:52 · 45m · Focus (pomodoro)");
    }

    private static PlannerMultiDayTimelineRenderer CreateRenderer(int width, Func<DateTime> nowProvider)
    {
        var lineRenderer = new PlannerTimelineLineRenderer();
        return new PlannerMultiDayTimelineRenderer(
            () => width, nowProvider, lineRenderer, new PlannerTimelineViewport(),
            new PlannerMultiDayCellRenderer(lineRenderer));
    }

    private static TodoItem Todo(string title) => new(
        1, false, null, title, null, [], null, null, string.Empty, [], []);

    private static PlannerView MultiDayView(
        DateOnly date,
        IReadOnlyList<PlannerDayColumnView> columns,
        int selectedSlot) => new(
            PlannerState.CreateInitial(date) with
            {
                ViewMode = PlannerViewMode.MultiDay,
                SlotIndex = selectedSlot
            },
            columns[0].Slots,
            [],
            [])
        {
            DayColumns = columns.ToImmutableArray()
        };

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

    private static string Render(IRenderable renderable, int width)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = width;
        console.Write(renderable);
        return writer.ToString();
    }

}
