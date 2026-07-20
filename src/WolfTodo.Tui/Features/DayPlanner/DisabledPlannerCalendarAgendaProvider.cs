using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DisabledPlannerCalendarAgendaProvider : IPlannerCalendarAgendaProvider
{
    public Task<PlannerCalendarAgenda> LoadAsync(
        GoogleCalendarConfiguration configuration,
        DateOnly date,
        CancellationToken cancellationToken) => Task.FromResult(PlannerCalendarAgenda.Disabled);
}
