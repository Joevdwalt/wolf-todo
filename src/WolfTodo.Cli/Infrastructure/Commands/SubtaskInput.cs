namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class SubtaskInput
{
    public string? Title { get; init; }
    public bool? Completed { get; init; }
}
