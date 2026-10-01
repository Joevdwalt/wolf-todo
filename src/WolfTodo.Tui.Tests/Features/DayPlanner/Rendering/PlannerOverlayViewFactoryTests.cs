using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

public sealed class PlannerOverlayViewFactoryTests
{
    [Fact]
    public void PlannerSelectList_uses_the_current_unscheduled_todo_filter()
    {
        var date = new DateOnly(2026, 8, 4);
        var view = new PlannerView(
            PlannerState.CreateInitial(date) with
            {
                Mode = PlannerMode.ChooseTodo,
                FilterText = "brief"
            },
            [new PlannerSlotView(new TimeOnly(9, 0), [], true)],
            [],
            []);
        var factory = new PlannerOverlayViewFactory(new StatusRenderer());

        var picker = factory.PlannerSelectList(view, TuiKeyBindings.CreateDefaults(":q"));

        picker.Should().NotBeNull();
        picker!.Title.Should().Be("Unscheduled todos");
        picker.SearchText.Should().Be("brief");
    }
}
