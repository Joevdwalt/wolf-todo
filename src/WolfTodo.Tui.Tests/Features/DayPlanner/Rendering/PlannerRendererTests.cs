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

public sealed class PlannerRendererTests
{
    [Fact]
    public void CreatePlannerRenderContext_calculates_wide_side_panel_layout()
    {
        var renderer = new PlannerRenderer(
            () => 140,
            () => 30,
            nowProvider: () => new DateTime(2026, 8, 4, 9, 0, 0));
        var view = new PlannerView(
            PlannerState.CreateInitial(new DateOnly(2026, 8, 4)) with { ShowDetails = true },
            [new PlannerSlotView(new TimeOnly(9, 0), [], true)],
            [],
            [])
        {
            OpenTodoCount = 2
        };

        var context = renderer.CreatePlannerRenderContext(view, TuiKeyBindings.CreateDefaults(":q"));

        context.Width.Should().Be(140);
        context.Height.Should().Be(30);
        context.WideSidePanels.Should().BeTrue();
        context.ShowAllDayPanel.Should().BeTrue();
        context.TimelineWidth.Should().Be(91);
        context.AvailableRows.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreatePlannerRenderContext_gives_multiday_columns_the_full_width()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = new[] { new PlannerSlotView(new TimeOnly(9, 0), [], true) };
        var view = new PlannerView(
            PlannerState.CreateInitial(date) with
            {
                ViewMode = PlannerViewMode.MultiDay,
                ShowDetails = true
            },
            slots.ToImmutableArray(),
            [],
            [])
        {
            DayColumns =
            [
                new PlannerDayColumnView(date, slots.ToImmutableArray(), PlannerCalendarAgenda.Disabled, true),
                new PlannerDayColumnView(date.AddDays(1), slots.ToImmutableArray(), PlannerCalendarAgenda.Disabled, false)
            ]
        };

        var context = new PlannerRenderer(() => 140, () => 30)
            .CreatePlannerRenderContext(view, TuiKeyBindings.CreateDefaults(":q"));

        context.WideSidePanels.Should().BeFalse();
        context.ShowAllDayPanel.Should().BeFalse("multiday panes render their own all-day rows");
        context.TimelineWidth.Should().Be(140);
    }

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

        var window = new PlannerRenderer().WindowPlannerMultiDaySlots(columns, 2, availableRows: 2);

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

        var window = new PlannerRenderer().WindowPlannerMultiDaySlots(columns, 12, availableRows: 8);

        window.Should().HaveCount(8);
        window.Should().Contain(12);
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
        var renderer = new PlannerRenderer(
            () => 80,
            () => 45,
            nowProvider: () => new DateTime(2026, 8, 4, 9, 15, 0));

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
        var renderer = new PlannerRenderer(
            () => 100,
            () => 45,
            nowProvider: () => new DateTime(2026, 8, 8, 9, 15, 0));

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
        var renderer = new PlannerRenderer(() => 100, () => 40,
            nowProvider: () => new DateTime(2026, 8, 8, 9, 15, 0));

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
        var renderer = new PlannerRenderer(
            () => 80,
            () => 45,
            nowProvider: () => new DateTime(2026, 8, 4, 8, 0, 0));

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
        var renderer = new PlannerRenderer(
            () => 80,
            () => 12,
            nowProvider: () => new DateTime(2026, 8, 4, 9, 0, 0));

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
        var renderer = new PlannerRenderer(() => 80, () => 45,
            nowProvider: () => new DateTime(2026, 8, 6, 9, 15, 0));

        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 30, TuiThemes.Wolf), 80);

        output.Should().Contain("T5").And.Contain("+4");
        output.Should().NotContain("T1").And.NotContain("T2");
        output.Split(Environment.NewLine).Should().NotContain(line => line.Trim() == "5",
            "multiday segments truncate instead of wrapping");
    }

    [Fact]
    public void One_date_multiday_view_keeps_multiday_layout_and_selected_summary_space()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(date, includeOverlap: false);
        var view = MultiDayView(date,
        [new PlannerDayColumnView(date, slots, PlannerCalendarAgenda.Disabled, true)],
            selectedSlot: 12);
        var renderer = new PlannerRenderer(() => 80, () => 40);

        var context = renderer.CreatePlannerRenderContext(view, TuiKeyBindings.CreateDefaults(":q"));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, context.AvailableRows, TuiThemes.Wolf), 80);

        context.WideSidePanels.Should().BeFalse();
        context.CompactDetails.Should().BeTrue();
        output.Should().Contain("▶ TUE 04").And.Contain("ALL DAY");
    }

    [Fact]
    public void WindowPlannerTimeline_inserts_now_marker_for_selected_today()
    {
        var renderer = new PlannerRenderer(
            () => 100,
            () => 30,
            nowProvider: () => new DateTime(2026, 8, 4, 9, 15, 0));
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
        var renderer = new PlannerRenderer(() => 100, () => 30);
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
        var renderer = new PlannerRenderer(() => 100, () => 30);
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

    [Fact]
    public void PlannerAvailableRows_reserves_status_picker_and_optional_panels()
    {
        var renderer = new PlannerRenderer();

        renderer.PlannerAvailableRows(
                terminalHeight: 30,
                statusHeight: 3,
                pickerHeight: 4,
                compactDetails: true,
                narrowAllDayHeight: 5)
            .Should().Be(7);
    }

    [Fact]
    public void PlannerDetailLines_shows_normal_details_for_a_selected_stacked_task()
    {
        var date = new DateOnly(2026, 8, 4);
        var schedule = new TodoSchedule(date, new TimeOnly(6, 0));
        var first = Todo("First") with { Schedule = schedule };
        var second = Todo("Second") with { SourceLine = 2, Schedule = schedule };
        var view = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/todos/work.md", [first, second])], []),
            PlannerState.CreateInitial(date) with
            {
                SelectedTimelineItemIdentity = "task:/todos/work.md:2"
            });

        var lines = new PlannerRenderer().PlannerDetailLines(view, TuiThemes.Wolf);

        lines.Should().HaveCountGreaterThan(2);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Inspector_and_compact_details_show_the_selected_task_link(bool allDay, bool multiday)
    {
        var date = new DateOnly(2026, 9, 14);
        var child = Todo("Child") with
        {
            SourceLine = 4,
            Schedule = new TodoSchedule(date, allDay ? null : new TimeOnly(6, 0))
        };
        var project = new TodoProject("Work", "/todos/work.md", [Todo("Parent") with { Subtasks = [child] }]);
        var view = new DayPlannerPresenter().CreateView(new ProjectCatalog([project], []),
            PlannerState.CreateInitial(date) with
            {
                Focus = allDay ? PlannerFocus.AllDay : PlannerFocus.Timeline,
                ViewMode = multiday ? PlannerViewMode.MultiDay : PlannerViewMode.SingleDay,
                VisibleDayCount = multiday ? 2 : 1
            });
        var renderer = new PlannerRenderer();
        using var writer = new StringWriter();
        var console = Spectre.Console.AnsiConsole.Create(new Spectre.Console.AnsiConsoleSettings
        {
            Ansi = Spectre.Console.AnsiSupport.No,
            Out = new Spectre.Console.AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 140;
        console.Write(new Spectre.Console.Rows(renderer.PlannerDetailLines(view, TuiThemes.Wolf)));
        writer.ToString().Should().Contain("LINK: " + TaskLinkCode.Generate(project.Path, child.SourceLine));
        writer.GetStringBuilder().Clear();
        console.Profile.Width = multiday ? 80 : 24;
        console.Write(renderer.PlannerCompactDetail(view, TuiThemes.Wolf));
        if (multiday)
        {
            writer.ToString().Should().Contain("MON 14").And.Contain("Child (todo)")
                .And.Contain(allDay ? "ALL DAY" : "06:00");
        }
        else
        {
            writer.ToString().Should().Contain("LINK: " + TaskLinkCode.Generate(project.Path, child.SourceLine));
        }
    }

    [Fact]
    public void Inspector_does_not_show_task_links_for_an_empty_timeslot()
    {
        var view = new DayPlannerPresenter().CreateView(new ProjectCatalog([], []),
            PlannerState.CreateInitial(new DateOnly(2026, 9, 14)));
        var lines = new PlannerRenderer().PlannerDetailLines(view, TuiThemes.Wolf);
        lines.Should().ContainSingle();
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
        var renderer = new PlannerRenderer(() => 80, () => 80,
            nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);
        var lines = output.Split(Environment.NewLine);

        lines.Single(line => line.Contains("│ 10:00") && line.Contains("→│10:10"))
            .Should().Contain("│").And.Contain("→│10:10");
        lines.Single(line => line.Contains("→│10:30"))
            .Should().Contain("→│10:30");
        Render(renderer.PlannerCompactDetail(view, TuiThemes.Wolf), 80)
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
        var renderer = new PlannerRenderer(() => 80, () => 80,
            nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);
        var lines = output.Split(Environment.NewLine);
        var endingRow = lines.Single(line => line.Contains("│ 10:00") && line.Contains("→│10:15"));

        endingRow.Split("→│10:15").Length.Should().Be(3);
        lines.Single(line => line.Contains("│ 11:00") && line.Contains("Quick call"))
            .Should().Contain("→│11:15");
        lines.Count(line => line.Contains("│ 10:00")).Should().Be(1);
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
        var renderer = new PlannerRenderer(() => 80, () => 80,
            nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);

        output.Split(Environment.NewLine).Single(line => line.Contains("Late work"))
            .Should().Contain("Late work").And.NotContain("→│");
        Render(renderer.PlannerCompactDetail(view, TuiThemes.Wolf), 80)
            .Should().Contain("21:45–WED 05 01:45 · 240m");
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
        var renderer = new PlannerRenderer(() => 80, () => 80,
            nowProvider: () => new DateTime(2026, 8, 6));
        var output = Render(renderer.CreatePlannerMultiDayTimelineTable(view, 64, TuiThemes.Wolf), 80);

        output.Should().Contain("→│09:52");
        output.Split(Environment.NewLine).Single(line => line.Contains("Send email"))
            .Should().NotContain("→│");
        Render(renderer.PlannerCompactDetail(view, TuiThemes.Wolf), 80)
            .Should().Contain("09:07–09:52 · 45m · Focus (pomodoro)");
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
            includeOverlap ? index switch { 0 => "Alpha", 1 => "Beta", _ => $"T{index}" } : $"Task {index}",
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
