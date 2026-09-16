using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Infrastructure.Calendar;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class CalendarDiskCacheTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"wolf-calendar-{Guid.NewGuid():N}");
    private static readonly DateOnly Today = new(2026, 9, 9);
    private static readonly GoogleCalendarConfiguration Configuration = new(true, "/client.json");

    [Fact]
    public async Task Syncs_fifteen_days_and_loads_them_immediately_after_restart()
    {
        var store = new JsonPlannerCalendarCacheStore(Path.Combine(root, "calendar.json"));
        var provider = new Provider();
        using (var cache = new PlannerCalendarAgendaCache(provider, store, () => Today))
        {
            cache.EnsureWindow(Configuration);
            await WaitForCompletion(cache);
            provider.Dates.Should().BeEquivalentTo(Enumerable.Range(-7, 15).Select(Today.AddDays));
            cache.EnsureWindow(Configuration);
            provider.Dates.Should().HaveCount(15);
        }

        using var restarted = new PlannerCalendarAgendaCache(new Provider(), store, () => Today);
        restarted.GetAgenda(Configuration, Today).Meetings.Should().ContainSingle().Which.Title.Should().Be("Meeting");
        store.Load(Configuration).Should().HaveCount(15);
        store.Load(Configuration with { OAuthClientFile = "/another-client.json" }).Should().BeEmpty();
    }

    [Fact]
    public async Task Failed_refresh_preserves_cached_entries_and_successful_empty_refresh_removes_them()
    {
        var store = new JsonPlannerCalendarCacheStore(Path.Combine(root, "calendar.json"));
        var provider = new Provider();
        using var cache = new PlannerCalendarAgendaCache(provider, store, () => Today);
        cache.EnsureWindow(Configuration);
        await WaitForCompletion(cache);

        provider.Fail = true;
        cache.RefreshWindow(Configuration);
        await WaitForCompletion(cache);
        cache.GetAgenda(Configuration, Today).Meetings.Should().ContainSingle();
        cache.GetAgenda(Configuration, Today).SyncState.Should().Be(PlannerCalendarSyncState.Offline);
        store.Load(Configuration)[Today].Meetings.Should().ContainSingle();

        provider.Fail = false;
        provider.Empty = true;
        cache.RefreshWindow(Configuration);
        await WaitForCompletion(cache);
        cache.GetAgenda(Configuration, Today).Meetings.Should().BeEmpty();
        store.Load(Configuration)[Today].Meetings.Should().BeEmpty();
    }

    [Fact]
    public void Corrupt_cache_is_ignored()
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "calendar.json");
        File.WriteAllText(path, "{broken");
        new JsonPlannerCalendarCacheStore(path).Load(Configuration).Should().BeEmpty();
    }

    private static async Task WaitForCompletion(PlannerCalendarAgendaCache cache)
    {
        for (var attempt = 0; attempt < 200 && cache.IsRefreshing; attempt++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        cache.IsRefreshing.Should().BeFalse();
    }

    private sealed class Provider : IPlannerCalendarAgendaProvider
    {
        public System.Collections.Concurrent.ConcurrentBag<DateOnly> Dates { get; } = [];
        public bool Fail { get; set; }
        public bool Empty { get; set; }

        public Task<PlannerCalendarAgenda> LoadAsync(GoogleCalendarConfiguration configuration, DateOnly date,
            CancellationToken cancellationToken)
        {
            Dates.Add(date);
            if (Fail) throw new IOException("Offline");
            return Task.FromResult(new PlannerCalendarAgenda([], Empty ? [] :
                [new PlannerCalendarMeeting("Meeting", new TimeOnly(9, 0), new TimeOnly(10, 0))],
                PlannerCalendarSyncState.Ready));
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
