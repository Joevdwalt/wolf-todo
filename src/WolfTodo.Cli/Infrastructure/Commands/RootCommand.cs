using McMaster.Extensions.CommandLineUtils;
using WolfTodo.Cli.Infrastructure.Commands.Add;
using WolfTodo.Cli.Infrastructure.Commands.Import;
using WolfTodo.Cli.Infrastructure.Commands.List;

namespace WolfTodo.Cli.Infrastructure.Commands;

[Command(Name = "wtodo", Description = "Wolf Todo CLI")]
[Subcommand(typeof(AddCommand), typeof(ImportCommand), typeof(ListCommand))]
[SuppressDefaultHelpOption]
public sealed class RootCommand(IConsole console)
{
    public int OnExecute()
    {
        console.Out.WriteLine(CliHelpText.Text);
        return 0;
    }
}
