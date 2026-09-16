using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class PlannerCalendarAgendaCache(
    IPlannerCalendarAgendaProvider provider,
    IPlannerCalendarCacheStore? store = null,
    Func<DateOnly>? todayProvider = null) : IDisposable
{
    private readonly Func<DateOnly> todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
    private readonly SemaphoreSlim concurrency = new(2);
    private CancellationTokenSource cancellation = new();
    private GoogleCalendarConfiguration? currentConfiguration;
    private DateOnly? synchronizedDay;
    private readonly object gate = new();
    private readonly Dictionary<DateOnly, PlannerCalendarAgenda> agendas = [];
    private readonly Dictionary<DateOnly, Task> refreshTasks = [];
    private long generation;

    public void EnsureWindow(GoogleCalendarConfiguration configuration)
    {
        lock (gate)
        {
            Configure(configuration);
            if (!configuration.Enabled || synchronizedDay == todayProvider()) return;
            RefreshWindow(configuration);
        }
    }

    public void RefreshWindow(GoogleCalendarConfiguration configuration)
    {
        lock (gate)
        {
            Configure(configuration);
            if (!configuration.Enabled) return;
            var today = todayProvider();
            synchronizedDay = today;
            foreach (var date in agendas.Keys.Where(date => Math.Abs(date.DayNumber - today.DayNumber) > 7).ToArray())
                agendas.Remove(date);
            // Prioritize today, then nearby dates. Limit simultaneous network requests.
            foreach (var offset in Enumerable.Range(-7, 15).OrderBy(Math.Abs))
                Refresh(configuration, today.AddDays(offset));
        }
    }

    private void Configure(GoogleCalendarConfiguration configuration)
    {
        if (currentConfiguration is not null && currentConfiguration.Enabled == configuration.Enabled &&
            currentConfiguration.OAuthClientFile == configuration.OAuthClientFile &&
            currentConfiguration.AdditionalCalendarIds.SequenceEqual(configuration.AdditionalCalendarIds)) return;
        Reset();
        currentConfiguration = configuration;
        if (!configuration.Enabled || store is null) return;
        var today = todayProvider();
        foreach (var entry in store.Load(configuration))
            if (Math.Abs(entry.Key.DayNumber - today.DayNumber) <= 7)
                agendas[entry.Key] = entry.Value;
    }

    public bool IsRefreshing
    {
        get
        {
            lock (gate)
            {
                return refreshTasks.Values.Any(task => !task.IsCompleted);
            }
        }
    }

    public PlannerCalendarAgenda GetAgenda(GoogleCalendarConfiguration configuration, DateOnly date)
    {
        if (!configuration.Enabled)
        {
            return PlannerCalendarAgenda.Disabled;
        }

        lock (gate)
        {
            Configure(configuration);
            if (agendas.TryGetValue(date, out var agenda))
            {
                return IsRefreshRunning(date)
                    ? agenda with { SyncState = PlannerCalendarSyncState.Syncing, Error = null }
                    : agenda;
            }
        }

        Refresh(configuration, date);
        return PlannerCalendarAgenda.Syncing;
    }

    public void Refresh(GoogleCalendarConfiguration configuration, DateOnly date)
    {
        if (!configuration.Enabled)
        {
            return;
        }

        lock (gate)
        {
            Configure(configuration);
            if (IsRefreshRunning(date))
            {
                return;
            }

            var refreshGeneration = generation;
            var token = cancellation.Token;
            var task = Task.Run(() => LoadAndPublishAsync(configuration, date, refreshGeneration, token));
            refreshTasks[date] = task;
            _ = task.ContinueWith(
                completed => RemoveCompletedTask(date, completed),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    public void Reset()
    {
        lock (gate)
        {
            generation++;
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = new CancellationTokenSource();
            currentConfiguration = null;
            synchronizedDay = null;
            agendas.Clear();
            refreshTasks.Clear();
        }
    }

    private async Task LoadAndPublishAsync(
        GoogleCalendarConfiguration configuration,
        DateOnly date,
        long refreshGeneration,
        CancellationToken cancellationToken)
    {
        PlannerCalendarAgenda agenda;
        try
        {
            await concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                agenda = await provider.LoadAsync(configuration, date, cancellationToken).ConfigureAwait(false);
            }
            finally { concurrency.Release(); }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
        catch (FileNotFoundException)
        {
            agenda = new PlannerCalendarAgenda(
                [], [], PlannerCalendarSyncState.ConfigurationError,
                "Google OAuth client file was not found.");
        }
        catch (UnauthorizedAccessException)
        {
            agenda = new PlannerCalendarAgenda(
                [], [], PlannerCalendarSyncState.AuthenticationRequired,
                "Google Calendar authorization is required.");
        }
        catch (Exception)
        {
            agenda = new PlannerCalendarAgenda(
                [], [], PlannerCalendarSyncState.Offline,
                "Google Calendar is unavailable.");
        }

        lock (gate)
        {
            if (refreshGeneration == generation)
            {
                if (agenda.SyncState != PlannerCalendarSyncState.Ready && agendas.TryGetValue(date, out var previous))
                    agenda = previous with { SyncState = agenda.SyncState, Error = agenda.Error };
                agendas[date] = agenda;
                if (agenda.SyncState == PlannerCalendarSyncState.Ready && store is not null)
                {
                    var today = todayProvider();
                    store.Save(configuration, agendas.Where(entry =>
                            Math.Abs(entry.Key.DayNumber - today.DayNumber) <= 7)
                        .ToDictionary(entry => entry.Key, entry => entry.Value));
                }
            }
        }
    }

    private bool IsRefreshRunning(DateOnly date) =>
        refreshTasks.TryGetValue(date, out var task) && !task.IsCompleted;

    private void RemoveCompletedTask(DateOnly date, Task completed)
    {
        lock (gate)
        {
            if (refreshTasks.TryGetValue(date, out var current) && current == completed)
            {
                refreshTasks.Remove(date);
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            generation++;
            cancellation.Cancel();
            agendas.Clear();
            refreshTasks.Clear();
        }
    }
}
