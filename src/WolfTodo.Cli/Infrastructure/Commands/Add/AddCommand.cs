using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands.Add;

public sealed class AddCommand(AddCommandHandler handler, CliInvocation invocation)
{
    [Option("--project", CommandOptionType.MultipleValue)] public string[] Project { get; set; } = [];

    [Option("--title", CommandOptionType.MultipleValue)] public string[] Title { get; set; } = [];

    [Option("--reference", CommandOptionType.MultipleValue)] public string[] Reference { get; set; } = [];

    [Option("--priority", CommandOptionType.MultipleValue)] public string[] Priority { get; set; } = [];

    [Option("--tag", CommandOptionType.MultipleValue)] public string[] Tags { get; set; } = [];

    [Option("--scheduled", CommandOptionType.MultipleValue)] public string[] Scheduled { get; set; } = [];

    [Option("--time", CommandOptionType.MultipleValue)] public string[] Time { get; set; } = [];

    [Option("--duration-minutes", CommandOptionType.MultipleValue)] public string[] DurationMinutes { get; set; } = [];

    [Option("--content", CommandOptionType.MultipleValue)] public string[] Content { get; set; } = [];

    [Option("--subtask", CommandOptionType.MultipleValue)] public string[] Subtasks { get; set; } = [];

    [Option("--completed-subtask", CommandOptionType.MultipleValue)] public string[] CompletedSubtasks { get; set; } = [];

    public int OnExecute() => handler.Execute(this, invocation.Arguments);
}
