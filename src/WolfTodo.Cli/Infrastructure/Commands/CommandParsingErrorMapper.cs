using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands;

public static class CommandParsingErrorMapper
{
    public static CommandException Map(string[] args, CommandParsingException exception)
    {
        var missingValueOption = FindMissingValueOption(args);
        var isCommand = args.Length > 0 &&
                        !args[0].StartsWith("-", StringComparison.Ordinal) &&
                        !new[] { "add", "import", "list" }.Contains(args[0], StringComparer.OrdinalIgnoreCase);
        var code = missingValueOption is not null
            ? "missing_value"
            : isCommand ? "unknown_command" : "unknown_option";
        var message = missingValueOption is not null
            ? $"Option {missingValueOption} requires a value."
            : exception.Message;
        return new CommandException(code, message);
    }

    private static string? FindMissingValueOption(string[] args)
    {
        var valueOptions = new HashSet<string>(StringComparer.Ordinal)
        {
            "--project", "--title", "--reference", "--priority", "--scheduled",
            "--time", "--duration-minutes", "--content", "--subtask", "--completed-subtask", "--file"
        };

        for (var index = 1; index < args.Length; index++)
        {
            if (valueOptions.Contains(args[index]) &&
                (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal)))
            {
                return args[index];
            }
        }

        return null;
    }
}
