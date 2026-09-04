namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class TaskInput
{
    public string? Title { get; init; }
    public string? Reference { get; init; }
    public string? Priority { get; init; }
    public List<string?>? Tags { get; init; }
    public ScheduleInput? Schedule { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Content { get; init; }
    public List<SubtaskInput?>? Subtasks { get; init; }
}
