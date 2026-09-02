namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class AddOptions
{
    public string? Project { get; set; }
    public string? Title { get; set; }
    public string? Reference { get; set; }
    public string? Priority { get; set; }
    public List<string?> Tags { get; } = [];
    public string? Scheduled { get; set; }
    public string? Time { get; set; }
    public string? DurationMinutes { get; set; }
    public List<ContentInput> Content { get; } = [];
}
