using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

public sealed class PlannerTimelineLineRendererTests
{
    [Fact]
    public void PlannerTimelineRenderLine_keeps_the_selected_branch_and_title_together()
    {
        var item = new PlannerTimelineItemView(
            PlannerItemType.Task, "task:1", "Write brief", new TimeOnly(9, 0),
            new TimeOnly(9, 15), PlannerTimeShape.Duration, PlannerIntervalState.StartAndEnd,
            false, true);
        var slot = new PlannerSlotView(new TimeOnly(9, 0), [], true) { Items = [item] };
        var row = PlannerTimelineRenderModel.ForSlot(slot).Single();
        var renderer = new PlannerTimelineLineRenderer();

        var output = Render(renderer.PlannerTimelineRenderLine(row, TuiThemes.Wolf), 60);

        output.Should().Contain("├▶ ○ Write brief · 15m");
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
