using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;

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
        rows[1].Should().BeOfType<PlannerNowTimelineRow>()
            .Which.Time.Should().Be(new TimeOnly(9, 15));
        rows[2].Should().BeOfType<PlannerSlotTimelineRow>();
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
}
