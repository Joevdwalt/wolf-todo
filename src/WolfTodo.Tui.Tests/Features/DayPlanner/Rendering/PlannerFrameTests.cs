using System.Collections.Immutable;
using FluentAssertions;
using Spectre.Console;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

[CollectionDefinition("Planner frames", DisableParallelization = true)]
public sealed class PlannerFrameCollection;

[Collection("Planner frames")]
public sealed class PlannerFrameTests
{
    [Theory]
    [InlineData(50, 24, 1)]
    [InlineData(80, 24, 2)]
    [InlineData(80, 40, 2)]
    [InlineData(100, 40, 3)]
    public void Multiday_frame_keeps_date_lanes_and_panels_inside_the_viewport(int width, int height, int visibleDates)
    {
        var output = RenderFrame(width, height);
        var lines = output.TrimEnd('\r', '\n').Split(Environment.NewLine);
        lines.Should().HaveCountLessThanOrEqualTo(height - 1);
        lines.Should().OnlyContain(line => line.GetCellWidth() <= width);
        var heading = lines.Single(line => line.Contains("TIME ") && line.Contains("THU 03"));
        heading.Split('│').Count(part => part.Contains("SEP") || part.Contains("THU 03") || part.Contains("FRI 04") || part.Contains("SAT 05"))
            .Should().Be(visibleDates);
        heading.Should().Contain("│ ▶ THU 03");
        output.Should().Contain("ALL DAY").And.Contain("SELECTED").And.Contain("THU 03 · 09:30–10:30 · 60m");
        var topBorder = lines.First(line => line.StartsWith('┌') && line.Contains('┬'));
        var dateStarts = topBorder.Select((glyph, index) => (glyph, index))
            .Where(pair => pair.glyph == '┬').Select(pair => pair.index + 4)
            .Take(visibleDates).ToArray();
        var timeline = lines.SkipWhile(line => !line.Contains("TIME ")).Skip(2)
            .TakeWhile(line => !line.Contains("ALL DAY")).Where(line => line.StartsWith('│')).ToArray();
        foreach (var start in dateStarts)
        {
            timeline.Should().OnlyContain(line => line[start] == '│' || line[start] == '┣' ||
                                                line[start] == '○' || line[start] == '⬥',
                "every date aligns its guide, item markers, and NOW marker in one lane");
        }
    }

    [Fact]
    public void Multiday_places_item_symbols_and_duration_continuations_on_one_guide()
    {
        var output = RenderFrame(100, 40);
        output.Should().Contain("▶ ○ Write").And.Contain("⬥ Revi")
            .And.Contain("○ Call client").And.Contain("│")
            .And.Contain("◆ Birthday").And.Contain("○ All-day task").And.Contain("┊");
        var cellLines = string.Join('\n', output.Split(Environment.NewLine).Where(line => line.StartsWith('│')));
        cellLines.Should().NotContain("├").And.NotContain("└")
            .And.NotContain("[Work]").And.NotContain("l MOVE");
        output.Should().Contain("Enter MOVE");
        var lines = output.Split(Environment.NewLine);
        var startRow = Array.FindIndex(lines, line => line.Contains("Call client"));
        var border = lines.First(line => line.Contains('┬'));
        var dividers = border.Select((glyph, index) => (glyph, index)).Where(pair => pair.glyph == '┬').Select(pair => pair.index).ToArray();
        var durationPaneStart = dividers[1] + 1;
        var durationPaneWidth = dividers[2] - durationPaneStart;
        var overlapPaneStart = dividers[0] + 1;
        var overlapPaneWidth = dividers[1] - overlapPaneStart;
        lines[startRow + 1].Substring(overlapPaneStart, overlapPaneWidth)
            .Should().Contain("│ ┊  │", "overlapping durations retain separate paths");
        lines[startRow + 2].Substring(overlapPaneStart, overlapPaneWidth)
            .Should().Contain("→│10:15", "the meeting finishes on its last occupied row");
        lines[startRow + 3].Substring(overlapPaneStart, overlapPaneWidth)
            .Should().Contain("→│10:30", "the surviving todo returns to the date guide");
        lines[startRow + 1].Substring(durationPaneStart, durationPaneWidth).Trim().Should().Be("│");
        lines[startRow + 2].Substring(durationPaneStart, durationPaneWidth).Trim().Should().Be("→│10:15");
    }

    [Fact]
    public void Active_all_day_task_uses_the_same_selection_arrow_and_task_symbol()
    {
        var output = RenderFrame(80, 40, allDay: true);
        output.Should().Contain("▶ ○ All-day task").And.Contain("THU 03 · ALL DAY · 45m estimate · All-day task (todo)");
        output.Should().NotContain("> ○").And.NotContain("[Work]");
    }

    private static string RenderFrame(int width, int height, bool allDay = false)
    {
        var date = new DateOnly(2026, 9, 3);
        TodoItem Task(int line, string title, DateOnly scheduledDate, TimeOnly? time, TimeSpan? duration = null) =>
            new(line, false, null, title, null, [], null, null, string.Empty, [], [])
            {
                Schedule = new TodoSchedule(scheduledDate, time),
                Duration = duration
            };
        var catalog = new ProjectCatalog([new TodoProject("Work", "/fixtures/work.md",
        [
            Task(1, "Write brief", date, new TimeOnly(9, 30), TimeSpan.FromMinutes(60)),
            Task(2, "Call client", date.AddDays(1), new TimeOnly(9, 30), TimeSpan.FromMinutes(45)),
            Task(3, "All-day task", date, null, TimeSpan.FromMinutes(45))
        ])], []);
        var state = PlannerState.CreateInitial(date) with
        {
            ViewMode = PlannerViewMode.MultiDay,
            VisibleDayCount = 3,
            SlotIndex = 14,
            Focus = allDay ? PlannerFocus.AllDay : PlannerFocus.Timeline,
            AllDayIndex = allDay ? 1 : 0
        };
        var presenter = new DayPlannerPresenter();
        var agenda = new PlannerCalendarAgenda(
            [new PlannerCalendarAllDayItem("Birthday", PlannerCalendarItemKind.Event)],
            [new PlannerCalendarMeeting("Review", new TimeOnly(9, 30), new TimeOnly(10, 15))],
            PlannerCalendarSyncState.Ready);
        var view = presenter.CreateView(catalog, state, agenda);
        var columns = Enumerable.Range(0, 3).Select(offset =>
        {
            var day = offset == 0 ? view : presenter.CreateView(catalog, state with { SelectedDate = date.AddDays(offset) }, isActiveDate: false);
            return new PlannerDayColumnView(date.AddDays(offset), day.Slots, day.CalendarAgenda, offset == 0);
        }).ToImmutableArray();
        view = view with { DayColumns = columns };
        var original = AnsiConsole.Console;
        using var writer = new StringWriter();
        try
        {
            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Ansi = AnsiSupport.No, Out = new AnsiConsoleOutput(writer) });
            AnsiConsole.Profile.Width = width;
            AnsiConsole.Profile.Height = height;
            new PlannerRenderer(() => width, () => height, nowProvider: () => new DateTime(2026, 9, 3, 10, 51, 0))
                .ShowPlanner(new TabStripView([new TabItemView(new TabId("planner"), "Day Planner", true)]),
                    view, TuiKeyBindings.CreateDefaults(":q"), TuiThemes.Wolf);
            return writer.ToString();
        }
        finally
        {
            AnsiConsole.Console = original;
        }
    }
}
