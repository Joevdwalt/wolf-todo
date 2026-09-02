using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands.Import;

public sealed class ImportCommand(ImportCommandHandler handler, CliInvocation invocation)
{
    [Option("--file", CommandOptionType.MultipleValue)]
    public string[] File { get; set; } = [];

    [Option("--stdin", CommandOptionType.NoValue)]
    public bool Stdin { get; set; }

    public int OnExecute() => handler.Execute(this, invocation.Arguments);
}
