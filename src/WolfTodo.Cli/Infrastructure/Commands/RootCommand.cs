using McMaster.Extensions.CommandLineUtils;

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
