namespace WolfTodo.Cli.Infrastructure.Commands;

public static class CommandArguments
{
    public static string[] ForSubcommand(IReadOnlyList<string> arguments, string command)
    {
        var index = arguments.ToList().FindIndex(value =>
            string.Equals(value, command, StringComparison.OrdinalIgnoreCase));

        return index < 0 ? arguments.ToArray() : arguments.Skip(index + 1).ToArray();
    }
}
