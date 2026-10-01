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

public sealed class PlannerDetailRendererTests
{
    [Fact]
    public void PlannerDetailLines_shows_normal_details_for_a_selected_stacked_task()
    {
        var date = new DateOnly(2026, 8, 4);
        var schedule = new TodoSchedule(date, new TimeOnly(6, 0));
        var first = Todo("First") with { Schedule = schedule };
        var second = Todo("Second") with { SourceLine = 2, Schedule = schedule };
        var view = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/todos/work.md", [first, second])], []),
            PlannerState.CreateInitial(date) with
            {
                SelectedTimelineItemIdentity = "task:/todos/work.md:2"
            });

        var lines = new PlannerDetailRenderer().PlannerDetailLines(view, TuiThemes.Wolf);

        lines.Should().HaveCountGreaterThan(2);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Inspector_and_compact_details_show_the_selected_task_link(bool allDay, bool multiday)
    {
        var date = new DateOnly(2026, 9, 14);
        var child = Todo("Child") with
        {
            SourceLine = 4,
            Schedule = new TodoSchedule(date, allDay ? null : new TimeOnly(6, 0))
        };
        var project = new TodoProject("Work", "/todos/work.md", [Todo("Parent") with { Subtasks = [child] }]);
        var view = new DayPlannerPresenter().CreateView(new ProjectCatalog([project], []),
            PlannerState.CreateInitial(date) with
            {
                Focus = allDay ? PlannerFocus.AllDay : PlannerFocus.Timeline,
                ViewMode = multiday ? PlannerViewMode.MultiDay : PlannerViewMode.SingleDay,
                VisibleDayCount = multiday ? 2 : 1
            });
        var renderer = new PlannerDetailRenderer();
        using var writer = new StringWriter();
        var console = Spectre.Console.AnsiConsole.Create(new Spectre.Console.AnsiConsoleSettings
        {
            Ansi = Spectre.Console.AnsiSupport.No,
            Out = new Spectre.Console.AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 140;
        console.Write(new Spectre.Console.Rows(renderer.PlannerDetailLines(view, TuiThemes.Wolf)));
        writer.ToString().Should().Contain("LINK: " + TaskLinkCode.Generate(project.Path, child.SourceLine));
        writer.GetStringBuilder().Clear();
        console.Profile.Width = multiday ? 80 : 24;
        console.Write(renderer.PlannerCompactDetail(view, TuiThemes.Wolf));
        if (multiday)
        {
            writer.ToString().Should().Contain("MON 14").And.Contain("Child (todo)")
                .And.Contain(allDay ? "ALL DAY" : "06:00");
        }
        else
        {
            writer.ToString().Should().Contain("LINK: " + TaskLinkCode.Generate(project.Path, child.SourceLine));
        }
    }

    [Fact]
    public void Inspector_does_not_show_task_links_for_an_empty_timeslot()
    {
        var view = new DayPlannerPresenter().CreateView(new ProjectCatalog([], []),
            PlannerState.CreateInitial(new DateOnly(2026, 9, 14)));
        var lines = new PlannerDetailRenderer().PlannerDetailLines(view, TuiThemes.Wolf);
        lines.Should().ContainSingle();
    }

    private static TodoItem Todo(string title) => new(
        1, false, null, title, null, [], null, null, string.Empty, [], []);

}
