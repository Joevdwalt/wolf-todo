namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class ImportDocument
{
    public string? Project { get; init; }
    public List<TaskInput?>? Tasks { get; init; }
}
