using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

public sealed class PlannerSingleDayTimelineRendererTests
{
    [Fact]
    public void CreatePlannerTimelineTable_renders_a_timed_item_in_its_slot()
    {
        var item = new PlannerTimelineItemView(
            PlannerItemType.Task, "task:1", "Write brief", new TimeOnly(9, 0),
            new TimeOnly(9, 0), PlannerTimeShape.Instant, PlannerIntervalState.Instant,
            false, false);
        var slot = new PlannerSlotView(new TimeOnly(9, 0), [], false) { Items = [item] };
        var renderer = new PlannerSingleDayTimelineRenderer(
            new PlannerTimelineLineRenderer(), new PlannerTimelineViewport());

        var output = Render(renderer.CreatePlannerTimelineTable(
            [new PlannerSlotTimelineRow(slot)], 1, TuiThemes.Wolf), 60);

        output.Should().Contain("09:00").And.Contain("○ Write brief");
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
