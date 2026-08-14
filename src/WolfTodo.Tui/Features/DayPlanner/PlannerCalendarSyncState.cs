namespace WolfTodo.Tui.Features.DayPlanner;

public enum PlannerCalendarSyncState
{
    Disabled,
    Syncing,
    Ready,
    Offline,
    AuthenticationRequired,
    ConfigurationError
}
