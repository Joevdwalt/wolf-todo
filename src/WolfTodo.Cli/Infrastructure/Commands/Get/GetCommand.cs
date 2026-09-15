using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands.Get;

public sealed class GetCommand(GetCommandHandler handler)
{
    [Argument(0, Name = "task-code", Description = "The wt1- task link code.")]
    public string? Code { get; set; }

    public int OnExecute() => handler.Execute(this);
}
