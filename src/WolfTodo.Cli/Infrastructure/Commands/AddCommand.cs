using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class AddCommand(ICliCommandRunner runner, CliInvocation invocation)
{
    [Option("--project", CommandOptionType.MultipleValue)]
    public string[] Project { get; set; } = [];

    [Option("--title", CommandOptionType.MultipleValue)]
    public string[] Title { get; set; } = [];

    [Option("--reference", CommandOptionType.MultipleValue)]
    public string[] Reference { get; set; } = [];

    [Option("--priority", CommandOptionType.MultipleValue)]
    public string[] Priority { get; set; } = [];

    [Option("--tag", CommandOptionType.MultipleValue)]
    public string[] Tags { get; set; } = [];

    [Option("--scheduled", CommandOptionType.MultipleValue)]
    public string[] Scheduled { get; set; } = [];

    [Option("--time", CommandOptionType.MultipleValue)]
    public string[] Time { get; set; } = [];

    [Option("--duration-minutes", CommandOptionType.MultipleValue)]
    public string[] DurationMinutes { get; set; } = [];

    [Option("--note", CommandOptionType.MultipleValue)]
    public string[] Notes { get; set; } = [];

    [Option("--subtask", CommandOptionType.MultipleValue)]
    public string[] Subtasks { get; set; } = [];

    [Option("--completed-subtask", CommandOptionType.MultipleValue)]
    public string[] CompletedSubtasks { get; set; } = [];

    public int OnExecute() => runner.RunAdd(CommandArguments.ForSubcommand(invocation.Arguments, "add"));
}
