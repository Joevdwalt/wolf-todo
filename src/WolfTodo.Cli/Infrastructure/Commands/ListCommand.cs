using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class ListCommand(ICliCommandRunner runner, CliInvocation invocation)
{
    [Option("--project", CommandOptionType.MultipleValue)]
    public string[] Project { get; set; } = [];

    public int OnExecute() => runner.RunList(CommandArguments.ForSubcommand(invocation.Arguments, "list"));
}
