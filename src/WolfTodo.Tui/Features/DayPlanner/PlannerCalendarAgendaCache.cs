using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class PlannerCalendarAgendaCache(IPlannerCalendarAgendaProvider provider)
{
    private readonly Dictionary<DateOnly, PlannerCalendarAgenda> agendas = [];
    private Task? refreshTask;
    private DateOnly? refreshingDate;

    public bool IsRefreshing => refreshTask is { IsCompleted: false };

    public PlannerCalendarAgenda GetAgenda(GoogleCalendarConfiguration configuration, DateOnly date)
    {
        if (!configuration.Enabled)
        {
            return PlannerCalendarAgenda.Disabled;
        }

        if (!agendas.TryGetValue(date, out var agenda))
        {
            Refresh(configuration, date);
            return PlannerCalendarAgenda.Syncing;
        }

        return IsRefreshing && refreshingDate == date
            ? agenda with { SyncState = PlannerCalendarSyncState.Syncing, Error = null }
            : agenda;
    }

    public void Refresh(GoogleCalendarConfiguration configuration, DateOnly date)
    {
        if (!configuration.Enabled || IsRefreshing)
        {
            return;
        }

        refreshingDate = date;
        refreshTask = RefreshAsync(configuration, date);
    }

    private async Task RefreshAsync(GoogleCalendarConfiguration configuration, DateOnly date)
    {
        try
        {
            agendas[date] = await provider.LoadAsync(configuration, date, CancellationToken.None);
        }
        catch (FileNotFoundException)
        {
            agendas[date] = new PlannerCalendarAgenda(
                [], [], PlannerCalendarSyncState.ConfigurationError,
                "Google OAuth client file was not found.");
        }
        catch (UnauthorizedAccessException)
        {
            agendas[date] = new PlannerCalendarAgenda(
                [], [], PlannerCalendarSyncState.AuthenticationRequired,
                "Google Calendar authorization is required.");
        }
        catch (Exception)
        {
            agendas[date] = new PlannerCalendarAgenda(
                [], [], PlannerCalendarSyncState.Offline,
                "Google Calendar is unavailable.");
        }
        finally
        {
            refreshingDate = null;
        }
    }
}
