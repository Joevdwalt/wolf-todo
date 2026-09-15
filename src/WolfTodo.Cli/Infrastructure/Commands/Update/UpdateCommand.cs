using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands.Update;

public sealed class UpdateCommand(UpdateCommandHandler handler)
{
    [Argument(0, Name = "task-code", Description = "The wt1- task link code.")]
    public string? Code { get; set; }

    [Option("--completed", CommandOptionType.MultipleValue)] public string[] Completed { get; set; } = [];
    [Option("--title", CommandOptionType.MultipleValue)] public string[] Title { get; set; } = [];
    [Option("--reference", CommandOptionType.MultipleValue)] public string[] Reference { get; set; } = [];
    [Option("--clear-reference")] public bool ClearReference { get; set; }
    [Option("--priority", CommandOptionType.MultipleValue)] public string[] Priority { get; set; } = [];
    [Option("--clear-priority")] public bool ClearPriority { get; set; }
    [Option("--tag", CommandOptionType.MultipleValue)] public string[] Tags { get; set; } = [];
    [Option("--clear-tags")] public bool ClearTags { get; set; }
    [Option("--scheduled", CommandOptionType.MultipleValue)] public string[] Scheduled { get; set; } = [];
    [Option("--time", CommandOptionType.MultipleValue)] public string[] Time { get; set; } = [];
    [Option("--clear-time")] public bool ClearTime { get; set; }
    [Option("--clear-schedule")] public bool ClearSchedule { get; set; }
    [Option("--duration-minutes", CommandOptionType.MultipleValue)] public string[] DurationMinutes { get; set; } = [];
    [Option("--clear-duration")] public bool ClearDuration { get; set; }
    [Option("--content", CommandOptionType.MultipleValue)] public string[] Content { get; set; } = [];
    [Option("--clear-content")] public bool ClearContent { get; set; }

    public int OnExecute() => handler.Execute(this);
}
