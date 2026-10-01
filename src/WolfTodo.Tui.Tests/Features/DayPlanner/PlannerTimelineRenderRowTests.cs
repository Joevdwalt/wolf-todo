using FluentAssertions;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class PlannerTimelineRenderRowTests
{
    [Fact]
    public void IsEmpty_is_true_for_a_row_without_an_item_type()
    {
        var row = new PlannerTimelineRenderRow(
            "09:00", false, string.Empty, "│", string.Empty, string.Empty, string.Empty,
            false, false, false, null, null);

        row.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_is_false_for_an_item_row()
    {
        var row = new PlannerTimelineRenderRow(
            "09:00", false, string.Empty, "├─", "○", "Task", string.Empty,
            false, false, false, PlannerItemType.Task, PlannerIntervalState.Instant);

        row.IsEmpty.Should().BeFalse();
    }
}
