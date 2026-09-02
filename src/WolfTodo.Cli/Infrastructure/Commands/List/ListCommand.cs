using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands.List;

public sealed class ListCommand(ListCommandHandler handler)
{
    [Option("--project", CommandOptionType.MultipleValue)]
    public string[] Project { get; set; } = [];

    public int OnExecute() => handler.Execute(this);
}
