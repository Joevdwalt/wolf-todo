using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class PlannerCalendarAgendaCache(IPlannerCalendarAgendaProvider provider)
{
    private readonly object gate = new();
    private readonly Dictionary<DateOnly, PlannerCalendarAgenda> agendas = [];
    private readonly Dictionary<DateOnly, Task> refreshTasks = [];

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
            if (IsRefreshRunning(date))
            {
                return;
            }

            var task = Task.Run(() => LoadAndPublishAsync(configuration, date));
            refreshTasks[date] = task;
            _ = task.ContinueWith(
                completed => RemoveCompletedTask(date, completed),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private async Task LoadAndPublishAsync(GoogleCalendarConfiguration configuration, DateOnly date)
    {
        PlannerCalendarAgenda agenda;
        try
        {
            agenda = await provider.LoadAsync(configuration, date, CancellationToken.None)
                .ConfigureAwait(false);
        }
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
            agendas[date] = agenda;
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
}
