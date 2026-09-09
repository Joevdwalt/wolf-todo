using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public interface IPlannerCalendarCacheStore
{
    IReadOnlyDictionary<DateOnly, PlannerCalendarAgenda> Load(GoogleCalendarConfiguration configuration);
    void Save(GoogleCalendarConfiguration configuration, IReadOnlyDictionary<DateOnly, PlannerCalendarAgenda> agendas);
}
