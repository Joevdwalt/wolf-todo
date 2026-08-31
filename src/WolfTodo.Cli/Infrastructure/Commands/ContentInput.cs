namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class ContentInput
{
    public string? Type { get; init; }
    public string? Text { get; init; }
    public string? Title { get; init; }
    public bool? Completed { get; init; }
}