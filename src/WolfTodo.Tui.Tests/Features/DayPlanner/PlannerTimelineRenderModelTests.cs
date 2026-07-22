using FluentAssertions;
using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class PlannerTimelineRenderModelTests
{
    [Fact]
    public void ForSlot_formats_full_and_minor_time_ruler_rows_in_fixed_columns()
    {
        PlannerTimelineRenderModel.TimeRuler(new TimeOnly(6, 0)).Should().Be(("06:00", false));
        PlannerTimelineRenderModel.TimeRuler(new TimeOnly(6, 15)).Should().Be((string.Empty, true));
    }

    [Fact]
    public void ForSlot_renders_a_selected_empty_slot_with_spine_and_marker()
    {
        var row = PlannerTimelineRenderModel.ForSlot(new PlannerSlotView(new TimeOnly(16, 30), [], true)).Single();

        row.PrimaryBranchGlyph.Should().Be("├▶");
        row.Title.Should().BeEmpty();
        row.IsSelected.Should().BeTrue();
        row.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ForSlot_uses_consistent_compact_and_duration_branches()
    {
        var compact = Item("Quick call", PlannerIntervalState.StartAndEnd, TimeSpan.FromMinutes(15));
        var start = Item("Deep work", PlannerIntervalState.Start, TimeSpan.FromMinutes(60));
        var middle = Item("Deep work", PlannerIntervalState.Continue, TimeSpan.FromMinutes(60));
        var end = Item("Deep work", PlannerIntervalState.End, TimeSpan.FromMinutes(60));

        PlannerTimelineRenderModel.ForSlot(Slot(compact)).Single().Should().Match<PlannerTimelineRenderRow>(row =>
            row.ActivityBranchGlyph == "├─" && row.StatusGlyph == "○" && row.Metadata == "· 15m");
        PlannerTimelineRenderModel.ForSlot(Slot(start)).Single().ActivityBranchGlyph.Should().Be("├─");
        PlannerTimelineRenderModel.ForSlot(Slot(middle)).Single().PrimaryBranchGlyph.Should().Be("│");
        PlannerTimelineRenderModel.ForSlot(Slot(end)).Single().PrimaryBranchGlyph.Should().Be("└─");
    }

    [Fact]
    public void ForSlot_uses_meeting_glyph_and_selected_branch()
    {
        var item = Item("Standup", PlannerIntervalState.StartAndEnd, TimeSpan.FromMinutes(15)) with
        {
            ItemType = PlannerItemType.Meeting,
            IsSelected = true
        };

        var row = PlannerTimelineRenderModel.ForSlot(Slot(item)).Single();

        row.ActivityBranchGlyph.Should().Be("├▶");
        row.StatusGlyph.Should().Be("◆");
    }

    [Fact]
    public void ForSlot_uses_the_same_cursor_for_every_selected_duration_slot()
    {
        var continuation = Item("Deep work", PlannerIntervalState.Continue, TimeSpan.FromMinutes(60)) with
        {
            IsSelected = true
        };
        var end = Item("Deep work", PlannerIntervalState.End, TimeSpan.FromMinutes(60)) with
        {
            IsSelected = true
        };

        PlannerTimelineRenderModel.ForSlot(Slot(continuation)).Single().PrimaryBranchGlyph.Should().Be("├▶");
        PlannerTimelineRenderModel.ForSlot(Slot(end)).Single().PrimaryBranchGlyph.Should().Be("├▶");
    }

    [Fact]
    public void ForSlot_keeps_an_active_duration_highlighted_without_repeating_its_cursor()
    {
        var activeContinuation = Item("Deep work", PlannerIntervalState.Continue, TimeSpan.FromMinutes(60)) with
        {
            IsActive = true
        };
        var cursorContinuation = activeContinuation with { IsSelected = true };

        var activeRow = PlannerTimelineRenderModel.ForSlot(Slot(activeContinuation)).Single();
        var cursorRow = PlannerTimelineRenderModel.ForSlot(Slot(cursorContinuation)).Single();

        activeRow.PrimaryBranchGlyph.Should().Be("│");
        activeRow.IsPrimaryActive.Should().BeTrue();
        activeRow.IsSelected.Should().BeFalse();
        cursorRow.PrimaryBranchGlyph.Should().Be("├▶");
        cursorRow.IsPrimaryActive.Should().BeTrue();
    }

    [Fact]
    public void ForSlot_groups_same_time_items_without_repeating_time_or_minor_tick()
    {
        var items = new[]
        {
            Item("First", PlannerIntervalState.Instant, TimeSpan.Zero),
            Item("Selected", PlannerIntervalState.StartAndEnd, TimeSpan.FromMinutes(15)) with { IsSelected = true },
            Item("Third", PlannerIntervalState.Instant, TimeSpan.Zero),
            Item("Last", PlannerIntervalState.Instant, TimeSpan.Zero)
        };

        var rows = PlannerTimelineRenderModel.ForSlot(new PlannerSlotView(new TimeOnly(12, 30), [], false)
        {
            Items = [.. items]
        });

        rows.Select(row => row.TimeLabel).Should().Equal("12:30", string.Empty, string.Empty, string.Empty);
        rows.Should().OnlyContain(row => !row.IsMinorTimeTick);
        rows.Select(row => row.ActivityBranchGlyph).Should().Equal("├─", "├▶", "├─", "└─");
    }

    [Fact]
    public void ForSlot_uses_a_temporary_second_lane_only_for_a_duration_overlap()
    {
        var continuing = Item("Long task", PlannerIntervalState.Continue, TimeSpan.FromMinutes(60));
        var overlapping = Item("Interrupt", PlannerIntervalState.Instant, TimeSpan.Zero);

        var overlap = PlannerTimelineRenderModel.ForSlot(new PlannerSlotView(new TimeOnly(12, 30), [], false)
        {
            Items = [continuing, overlapping]
        }).Single();
        var ordinary = PlannerTimelineRenderModel.ForSlot(Slot(continuing)).Single();

        overlap.PrimaryBranchGlyph.Should().Be("│");
        overlap.ActivityBranchGlyph.Should().Be("├─");
        ordinary.PrimaryBranchGlyph.Should().Be("│");
        ordinary.ActivityBranchGlyph.Should().BeEmpty();
    }

    private static PlannerSlotView Slot(PlannerTimelineItemView item) => new(new TimeOnly(9, 0), [], false)
    {
        Items = [item]
    };

    private static PlannerTimelineItemView Item(string title, PlannerIntervalState state, TimeSpan duration) => new(
        PlannerItemType.Task, title, title, new TimeOnly(9, 0), new TimeOnly(9, 0).Add(duration),
        PlannerTimeShape.Duration, state, false, false);
}
