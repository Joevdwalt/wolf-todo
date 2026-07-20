using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class PlannerCalendarAgendaCacheTests
{
    private static readonly GoogleCalendarConfiguration Configuration =
        new(true, Path.GetFullPath("google-client.json"));

    [Fact]
    public async Task GetAgenda_returns_syncing_without_waiting_for_the_provider()
    {
        var provider = new BlockingProvider();
        var cache = new PlannerCalendarAgendaCache(provider);
        var date = new DateOnly(2026, 7, 20);

        var agenda = cache.GetAgenda(Configuration, date);

        agenda.SyncState.Should().Be(PlannerCalendarSyncState.Syncing);
        await Eventually(() => provider.Started);
        cache.IsRefreshing.Should().BeTrue();

        provider.Complete(date);
        await Eventually(() => cache.GetAgenda(Configuration, date).SyncState == PlannerCalendarSyncState.Ready);
    }

    [Fact]
    public async Task Refresh_deduplicates_a_day_but_allows_another_day_to_load()
    {
        var provider = new BlockingProvider();
        var cache = new PlannerCalendarAgendaCache(provider);
        var first = new DateOnly(2026, 7, 20);
        var second = first.AddDays(1);

        cache.Refresh(Configuration, first);
        cache.Refresh(Configuration, first);
        cache.Refresh(Configuration, second);

        await Eventually(() => provider.RequestCount == 2);
        provider.RequestedDates.Should().Contain([first, second]);

        provider.Complete(first);
        provider.Complete(second);
        await Eventually(() => !cache.IsRefreshing);
    }

    private static async Task Eventually(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        condition().Should().BeTrue("the background calendar operation should complete");
    }

    private sealed class BlockingProvider : IPlannerCalendarAgendaProvider
    {
        private readonly Dictionary<DateOnly, TaskCompletionSource<PlannerCalendarAgenda>> pending = [];
        private readonly object gate = new();

        public bool Started { get; private set; }

        public List<DateOnly> RequestedDates { get; } = [];

        public int RequestCount
        {
            get
            {
                lock (gate)
                {
                    return RequestedDates.Count;
                }
            }
        }

        public Task<PlannerCalendarAgenda> LoadAsync(
            GoogleCalendarConfiguration configuration,
            DateOnly date,
            CancellationToken cancellationToken)
        {
            lock (gate)
            {
                Started = true;
                RequestedDates.Add(date);
                var completion = new TaskCompletionSource<PlannerCalendarAgenda>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                pending[date] = completion;
                return completion.Task;
            }
        }

        public void Complete(DateOnly date)
        {
            lock (gate)
            {
                pending[date].SetResult(new PlannerCalendarAgenda(
                    [], [], PlannerCalendarSyncState.Ready));
            }
        }
    }
}
