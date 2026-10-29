using System.Collections.Concurrent;
using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class PlannerCalendarWorkflowTests
{
    private static readonly DateOnly Today = new(2026, 9, 9);
    private static readonly ApplicationConfiguration Configuration = new([], ":q")
    {
        GoogleCalendar = new GoogleCalendarConfiguration(true, "/client.json")
    };

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Columns_load_and_refresh_their_own_calendar_agendas(int count)
    {
        var provider = new CalendarProvider();
        using var cache = new PlannerCalendarAgendaCache(provider, todayProvider: () => Today);
        var workflow = new PlannerWorkflow(new DayPlannerPresenter(), new DayPlannerReducer(), cache,
            null, null, null!, null!);
        var start = Today.AddDays(20);
        var state = PlannerState.CreateInitial(start) with
        {
            ViewMode = count == 1 ? PlannerViewMode.SingleDay : PlannerViewMode.MultiDay,
            VisibleDayCount = count,
            VisibleStartDate = start
        };
        var catalog = new ProjectCatalog([], []);

        workflow.CreateView(catalog, state, Configuration, null);
        await WaitForCompletion(cache);
        var view = workflow.CreateView(catalog, state, Configuration, null);

        view.DayColumns.Should().HaveCount(count);
        foreach (var column in view.DayColumns)
        {
            column.CalendarAgenda.SyncState.Should().Be(PlannerCalendarSyncState.Ready);
            column.CalendarAgenda.AllDayItems.Should().ContainSingle(item => item.Title == $"All day {column.Date:yyyy-MM-dd}");
            column.Slots.Should().Contain(slot => slot.Meetings.Any(meeting => meeting.Title == $"Meeting {column.Date:yyyy-MM-dd}"));
        }

        var before = provider.Dates.ToArray();
        workflow.Refresh(Configuration, state);
        await WaitForCompletion(cache);
        foreach (var date in Enumerable.Range(0, count).Select(start.AddDays))
            provider.Dates.Count(requested => requested == date).Should().Be(before.Count(requested => requested == date) + 1);
    }

    private static async Task WaitForCompletion(PlannerCalendarAgendaCache cache)
    {
        for (var attempt = 0; attempt < 300 && cache.IsRefreshing; attempt++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        cache.IsRefreshing.Should().BeFalse();
    }

    private sealed class CalendarProvider : IPlannerCalendarAgendaProvider
    {
        public ConcurrentBag<DateOnly> Dates { get; } = [];

        public Task<PlannerCalendarAgenda> LoadAsync(GoogleCalendarConfiguration configuration, DateOnly date,
            CancellationToken cancellationToken)
        {
            Dates.Add(date);
            return Task.FromResult(new PlannerCalendarAgenda(
                [new PlannerCalendarAllDayItem($"All day {date:yyyy-MM-dd}", PlannerCalendarItemKind.Event)],
                [new PlannerCalendarMeeting($"Meeting {date:yyyy-MM-dd}", new TimeOnly(9, 0), new TimeOnly(10, 0))],
                PlannerCalendarSyncState.Ready));
        }
    }
}
