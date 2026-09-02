using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class ImportCommand(ICliCommandRunner runner, CliInvocation invocation)
{
    [Option("--file", CommandOptionType.MultipleValue)]
    public string[] File { get; set; } = [];

    [Option("--stdin", CommandOptionType.NoValue)]
    public bool Stdin { get; set; }

    public int OnExecute() => runner.RunImport(CommandArguments.ForSubcommand(invocation.Arguments, "import"));
}
