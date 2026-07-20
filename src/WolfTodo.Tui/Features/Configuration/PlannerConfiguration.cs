namespace WolfTodo.Tui.Features.Configuration;

public sealed record PlannerConfiguration(int DefaultDurationMinutes)
{
    public static PlannerConfiguration Default { get; } = new(30);

    public TimeSpan DefaultDuration => TimeSpan.FromMinutes(DefaultDurationMinutes);
}
