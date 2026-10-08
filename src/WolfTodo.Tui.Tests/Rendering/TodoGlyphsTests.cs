using FluentAssertions;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Tests.Rendering;

public sealed class TodoGlyphsTests
{
    [Fact]
    public void TodoGlyphs_exposes_shared_status_selection_and_mark_glyphs()
    {
        TodoGlyphs.OpenTodoGlyph.Should().Be("◯");
        TodoGlyphs.CompletedTodoGlyph.Should().Be("✓");
        TodoGlyphs.SelectedGlyph.Should().Be(">");
        TodoGlyphs.MarkedGlyph.Should().Be("*");
        TodoGlyphs.ScheduleGlyph.Should().Be("⏳");
        TodoGlyphs.PomodoroGlyph.Should().Be("◷");
        TodoGlyphs.TreeBranchGlyph.Should().Be("├─");
        TodoGlyphs.TreeContinuationGlyph.Should().Be("│");
        TodoGlyphs.TreeEndBranchGlyph.Should().Be("└─");
        TodoGlyphs.NoteBullet.Should().Be("•");
    }

    [Fact]
    public void TodoGlyphs_exposes_planner_glyphs()
    {
        TodoGlyphs.PlannerOpenTaskStatus.Should().Be("○");
        TodoGlyphs.PlannerMeetingStatus.Should().Be("⬥");
        TodoGlyphs.PlannerCalendarItem.Should().Be("◆");
        TodoGlyphs.PlannerMeetingWarning.Should().Be("⚠");
        TodoGlyphs.PlannerSelectedPointer.Should().Be("▶");
        TodoGlyphs.PlannerSelectedBranch.Should().Be("├▶");
        TodoGlyphs.PlannerTimeTick.Should().Be("—");
        TodoGlyphs.PlannerFinishArrow.Should().Be("→");
        TodoGlyphs.PlannerNowMarkerPrefix.Should().Be("┣━━");
    }
}
