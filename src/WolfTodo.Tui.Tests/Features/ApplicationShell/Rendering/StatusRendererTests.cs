using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell.Rendering;

public sealed class StatusRendererTests
{
    private readonly StatusRenderer renderer = new();

    [Fact]
    public void Wrap_splits_long_status_text_inside_width()
    {
        renderer.Wrap("alpha beta gamma", 10)
            .Should().Equal("alpha", "beta gamma");
    }

    [Fact]
    public void SortHint_reports_source_sort_as_generic_sort()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q");

        renderer.SortHint(BrowserState.Initial, bindings).Should().Be("t SORT");
    }

    [Fact]
    public void BrowserMode_reports_command_and_error_modes_before_browse()
    {
        var view = new BrowserView(
            BrowserState.Initial,
            [],
            [],
            null,
            "All",
            null,
            null,
            "Empty");

        renderer.BrowserMode(view).Should().Be("BROWSE");
        renderer.BrowserMode(view with { GlobalCommand = ":q" }).Should().Be("COMMAND");
        renderer.BrowserMode(view with { GlobalError = "Nope" }).Should().Be("ERROR");
    }

    [Fact]
    public void CalendarStatus_maps_calendar_sync_state()
    {
        renderer.CalendarStatus(PlannerCalendarAgenda.Syncing).Should().Be("SYNCING");
        renderer.CalendarStatus(new PlannerCalendarAgenda([], [], PlannerCalendarSyncState.Ready))
            .Should().Be("READY");
    }

    [Fact]
    public void PlannerStatus_reports_calendar_error_with_retry_hint()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q");
        var view = new PlannerView(
            PlannerState.CreateInitial(new DateOnly(2026, 8, 4)),
            [new PlannerSlotView(new TimeOnly(9, 0), [], true)],
            [],
            [])
        {
            CalendarAgenda = new PlannerCalendarAgenda([], [], PlannerCalendarSyncState.Offline, "Calendar unavailable")
        };

        renderer.PlannerStatus(view, bindings, 120, 30)
            .Should().ContainSingle()
            .Which.Text.Should().Contain("Calendar unavailable").And.Contain("r RETRY");
    }
}
