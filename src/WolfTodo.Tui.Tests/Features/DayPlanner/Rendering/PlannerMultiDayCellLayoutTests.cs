using FluentAssertions;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

public sealed class PlannerMultiDayCellLayoutTests
{
    private static readonly DateOnly Date = new(2026, 8, 4);
    private readonly PlannerMultiDayCellLayout layout = new();

    [Fact]
    public void Create_preserves_equal_start_order_and_uses_natural_widths()
    {
        var slot = Slot(
            Item("First", new TimeOnly(9, 0)),
            Item("Second", new TimeOnly(9, 0)));

        var plan = layout.Create(slot, PlannerTimelineRenderModel.ForSlot(slot), Date, 50, null);

        plan.VisibleRows.Select(row => row.Title).Should().Equal("First", "Second");
        plan.SegmentWidths.Should().Equal(9, 10);
        plan.HiddenCount.Should().Be(0);
    }

    [Fact]
    public void Create_keeps_the_title_width_for_an_overlapping_continuation()
    {
        var followup = Item("Followup with frans on quote", new TimeOnly(11, 30), duration: true);
        var meeting = Item("Business Development", new TimeOnly(11, 30), duration: true);
        var starts = Slot(
            followup with { IntervalState = PlannerIntervalState.Start },
            meeting with { IntervalState = PlannerIntervalState.Start });
        var ends = Slot(
            followup with { IntervalState = PlannerIntervalState.End },
            meeting with { IntervalState = PlannerIntervalState.Continue });

        var startPlan = layout.Create(starts, PlannerTimelineRenderModel.ForSlot(starts), Date, 100, null);
        var endPlan = layout.Create(ends, PlannerTimelineRenderModel.ForSlot(ends), Date, 100, null);

        endPlan.SegmentWidths.Should().Equal(startPlan.SegmentWidths);
        endPlan.SegmentWidths[0].Should().BeGreaterThan(" →│11:45".Length);
        endPlan.VisibleRows[0].BranchGlyph.Should().Be(" →│11:45");
    }

    [Fact]
    public void Create_keeps_selected_overlap_visible_and_counts_hidden_items()
    {
        var items = Enumerable.Range(0, 6)
            .Select(index => Item($"T{index}", new TimeOnly(9, 0), selected: index == 5))
            .ToArray();
        var slot = Slot(items);

        var plan = layout.Create(slot, PlannerTimelineRenderModel.ForSlot(slot), Date, 32, null);

        plan.VisibleRows.Select(row => row.Title).Should().Equal("T4", "T5");
        plan.SegmentWidths.Should().Equal(6, 7, 3);
        plan.HiddenCount.Should().Be(4);
    }

    [Theory]
    [InlineData(10, "A lon…", "")]
    [InlineData(11, "", "→│09:15")]
    [InlineData(12, "", " →│09:15")]
    [InlineData(13, "…", "→│09:15")]
    public void Create_reserves_the_finish_cue_before_truncating_the_title(
        int width, string title, string cue)
    {
        var slot = Slot(Item("A long task", new TimeOnly(9, 0), duration: true));

        var plan = layout.Create(slot, PlannerTimelineRenderModel.ForSlot(slot), Date, width, null);

        plan.VisibleRows[0].Title.Should().Be(title);
        plan.VisibleRows[0].Metadata.Should().Be(cue);
        plan.SegmentWidths.Should().Equal(width);
    }

    [Fact]
    public void Create_truncates_wide_unicode_title_by_terminal_cell_width()
    {
        var slot = Slot(Item("漢字 test", new TimeOnly(9, 0), duration: true));

        var plan = layout.Create(slot, PlannerTimelineRenderModel.ForSlot(slot), Date, 16, null);

        plan.VisibleRows[0].Title.TrimEnd().Should().Be("漢…");
        plan.VisibleRows[0].Metadata.Should().Be("→│09:15");
    }

    private static PlannerSlotView Slot(params PlannerTimelineItemView[] items) =>
        new(new TimeOnly(9, 0), [], false) { Items = [.. items] };

    private static PlannerTimelineItemView Item(
        string title, TimeOnly start, bool selected = false, bool duration = false) =>
        new(PlannerItemType.Task, $"task:{title}", title, start, start.AddMinutes(15),
            duration ? PlannerTimeShape.Duration : PlannerTimeShape.Instant,
            duration ? PlannerIntervalState.StartAndEnd : PlannerIntervalState.Instant,
            false, selected, IsActive: selected);
}
