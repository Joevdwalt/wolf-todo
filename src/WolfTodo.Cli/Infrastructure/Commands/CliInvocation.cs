namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class CliInvocation(IReadOnlyList<string> arguments)
{
    public IReadOnlyList<string> Arguments { get; } = arguments;
}
