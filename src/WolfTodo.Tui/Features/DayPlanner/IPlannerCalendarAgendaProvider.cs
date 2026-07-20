using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public interface IPlannerCalendarAgendaProvider
{
    Task<PlannerCalendarAgenda> LoadAsync(
        GoogleCalendarConfiguration configuration,
        DateOnly date,
        CancellationToken cancellationToken);
}
