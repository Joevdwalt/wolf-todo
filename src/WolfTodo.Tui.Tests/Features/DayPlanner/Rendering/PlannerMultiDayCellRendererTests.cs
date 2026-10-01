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

public sealed class PlannerMultiDayCellRendererTests
{
    [Theory]
    [InlineData(false, "  │")]
    [InlineData(true, "▶ │")]
    public void PlannerMultiDayTimelineCell_renders_an_empty_slot_guide(bool selected, string expected)
    {
        var slot = new PlannerSlotView(new TimeOnly(9, 0), [], selected);
        var rows = PlannerTimelineRenderModel.ForSlot(slot);
        var renderer = new PlannerMultiDayCellRenderer(new PlannerTimelineLineRenderer());

        var output = Render(renderer.PlannerMultiDayTimelineCell(
            slot, rows, new DateOnly(2026, 8, 4), 40, TuiThemes.Wolf, null), 40);

        output.Should().Contain(expected);
    }

    [Fact]
    public void PlannerMultiDayTimelineCell_renders_finish_time_and_truncates_a_compact_title()
    {
        var item = new PlannerTimelineItemView(
            PlannerItemType.Task,
            "task:compact",
            "A very long task title",
            new TimeOnly(9, 0),
            new TimeOnly(9, 15),
            PlannerTimeShape.Duration,
            PlannerIntervalState.StartAndEnd,
            false,
            false);
        var slot = new PlannerSlotView(new TimeOnly(9, 0), [], false) { Items = [item] };
        var rows = PlannerTimelineRenderModel.ForSlot(slot);
        var renderer = new PlannerMultiDayCellRenderer(new PlannerTimelineLineRenderer());

        var output = Render(renderer.PlannerMultiDayTimelineCell(
            slot, rows, new DateOnly(2026, 8, 4), 24, TuiThemes.Wolf, null), 24);

        output.Should().Contain("→│09:15").And.Contain("…");
    }

    [Fact]
    public void PlannerMultiDayTimelineCell_keeps_an_exact_fit_finish_cue_on_one_line()
    {
        var item = new PlannerTimelineItemView(
            PlannerItemType.Task, "task:narrow", "A long task", new TimeOnly(9, 0),
            new TimeOnly(9, 15), PlannerTimeShape.Duration,
            PlannerIntervalState.StartAndEnd, false, false);
        var slot = new PlannerSlotView(new TimeOnly(9, 0), [], false) { Items = [item] };
        var renderer = new PlannerMultiDayCellRenderer(new PlannerTimelineLineRenderer());

        var output = Render(renderer.PlannerMultiDayTimelineCell(
            slot, PlannerTimelineRenderModel.ForSlot(slot), new DateOnly(2026, 8, 4),
            11, TuiThemes.Wolf, null), 11);

        output.TrimEnd('\r', '\n').Should().Be("  ○ →│09:15");
    }

    [Fact]
    public void PlannerMultiDayTimelineCell_aligns_overlap_separator_with_the_start_title()
    {
        var followup = new PlannerTimelineItemView(
            PlannerItemType.Task, "task:followup", "Followup with frans on quote",
            new TimeOnly(11, 30), new TimeOnly(12, 0),
            PlannerTimeShape.Duration, PlannerIntervalState.Start, false, false);
        var meeting = new PlannerTimelineItemView(
            PlannerItemType.Meeting, "meeting:business", "Business Development Partner Programme",
            new TimeOnly(11, 30), new TimeOnly(12, 30),
            PlannerTimeShape.Duration, PlannerIntervalState.Start, false, false);
        var start = new PlannerSlotView(new TimeOnly(11, 30), [], false) { Items = [followup, meeting] };
        var finish = new PlannerSlotView(new TimeOnly(11, 45), [], false)
        {
            Items = [followup with { IntervalState = PlannerIntervalState.End },
                meeting with { IntervalState = PlannerIntervalState.Continue }]
        };
        var renderer = new PlannerMultiDayCellRenderer(new PlannerTimelineLineRenderer());

        var titleLine = Render(renderer.PlannerMultiDayTimelineCell(start,
            PlannerTimelineRenderModel.ForSlot(start), new DateOnly(2026, 8, 4),
            100, TuiThemes.Wolf, null), 100);
        var finishLine = Render(renderer.PlannerMultiDayTimelineCell(finish,
            PlannerTimelineRenderModel.ForSlot(finish), new DateOnly(2026, 8, 4),
            100, TuiThemes.Wolf, null), 100);

        finishLine.Should().Contain("→│12:00");
        finishLine.IndexOf('┊').Should().Be(titleLine.IndexOf('┊'));
    }

    [Fact]
    public void PlannerMultiDayTimelineCell_keeps_the_selected_item_visible_when_overlaps_overflow()
    {
        var date = new DateOnly(2026, 8, 4);
        var slot = MultiDaySlots(date, includeOverlap: true, itemCount: 6)[12];
        var rows = PlannerTimelineRenderModel.ForSlot(slot);
        var renderer = new PlannerMultiDayCellRenderer(new PlannerTimelineLineRenderer());

        var output = Render(renderer.PlannerMultiDayTimelineCell(
            slot, rows, date, 32, TuiThemes.Wolf, null), 32);

        output.Should().Contain("T5").And.Contain("+4").And.Contain("┊");
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
