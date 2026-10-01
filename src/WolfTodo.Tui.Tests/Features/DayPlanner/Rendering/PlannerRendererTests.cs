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
    public void CreatePlannerRenderContext_gives_multiday_columns_the_full_width()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = new[] { new PlannerSlotView(new TimeOnly(9, 0), [], true) };
        var view = new PlannerView(
            PlannerState.CreateInitial(date) with
            {
                ViewMode = PlannerViewMode.MultiDay,
                ShowDetails = true
            },
            slots.ToImmutableArray(),
            [],
            [])
        {
            DayColumns =
            [
                new PlannerDayColumnView(date, slots.ToImmutableArray(), PlannerCalendarAgenda.Disabled, true),
                new PlannerDayColumnView(date.AddDays(1), slots.ToImmutableArray(), PlannerCalendarAgenda.Disabled, false)
            ]
        };

        var context = new PlannerRenderer(() => 140, () => 30)
            .CreatePlannerRenderContext(view, TuiKeyBindings.CreateDefaults(":q"));

        context.WideSidePanels.Should().BeFalse();
        context.ShowAllDayPanel.Should().BeFalse("multiday panes render their own all-day rows");
        context.TimelineWidth.Should().Be(140);
    }

    [Fact]
    public void One_date_multiday_view_keeps_multiday_layout_and_selected_summary_space()
    {
        var date = new DateOnly(2026, 8, 4);
        var slots = MultiDaySlots(date, includeOverlap: false);
        var view = MultiDayView(date,
        [new PlannerDayColumnView(date, slots, PlannerCalendarAgenda.Disabled, true)],
            selectedSlot: 12);
        var renderer = new PlannerRenderer(() => 80, () => 40);

        var context = renderer.CreatePlannerRenderContext(view, TuiKeyBindings.CreateDefaults(":q"));
        var lineRenderer = new PlannerTimelineLineRenderer();
        var tableRenderer = new PlannerMultiDayTimelineRenderer(
            () => 80, () => new DateTime(2026, 8, 6), lineRenderer,
            new PlannerTimelineViewport(), new PlannerMultiDayCellRenderer(lineRenderer));
        var output = Render(tableRenderer.CreatePlannerMultiDayTimelineTable(
            view, context.AvailableRows, TuiThemes.Wolf), 80);

        context.WideSidePanels.Should().BeFalse();
        context.CompactDetails.Should().BeTrue();
        output.Should().Contain("▶ TUE 04").And.Contain("ALL DAY");
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

    private static PlannerView MultiDayView(
        DateOnly date,
        IReadOnlyList<PlannerDayColumnView> columns,
        int selectedSlot) => new(
            PlannerState.CreateInitial(date) with
            {
                ViewMode = PlannerViewMode.MultiDay,
                SlotIndex = selectedSlot
            },
            columns[0].Slots,
            [],
            [])
        {
            DayColumns = columns.ToImmutableArray()
        };

    private static ImmutableArray<PlannerSlotView> MultiDaySlots(
        DateOnly date,
        bool includeOverlap,
        int itemCount = 2)
    {
        var slots = Enumerable.Range(0, DayPlannerPresenter.SlotCount)
            .Select(index => new PlannerSlotView(new TimeOnly(6, 0).AddMinutes(index * 15), [], index == 12))
            .ToArray();
        var overlapItems = Enumerable.Range(0, itemCount).Select(index => new PlannerTimelineItemView(
            PlannerItemType.Task,
            $"task:{index}",
            includeOverlap ? index switch { 0 => "Alpha", 1 => "Beta", int otherIndex => $"T{otherIndex}" } : $"Task {index}",
            new TimeOnly(9, 0),
            new TimeOnly(9, 15),
            PlannerTimeShape.Instant,
            PlannerIntervalState.Instant,
            false,
            index == itemCount - 1,
            IsActive: index == itemCount - 1)).ToImmutableArray();
        slots[12] = slots[12] with { Items = overlapItems };
        return slots.ToImmutableArray();
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
