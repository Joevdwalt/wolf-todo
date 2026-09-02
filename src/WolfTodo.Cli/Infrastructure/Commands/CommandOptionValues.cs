namespace WolfTodo.Cli.Infrastructure.Commands;

public static class CommandOptionValues
{
    public static string? OptionalSingle(string[] values, string option)
    {
        if (values.Length > 1)
        {
            throw new CommandException("duplicate_option", $"Option {option} may only be specified once.");
        }

        return values.SingleOrDefault();
    }

    public static string RequiredSingle(string[] values, string option)
    {
        var value = OptionalSingle(values, option);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CommandException("missing_option", $"Option {option} is required.");
        }

        return value;
    }
}
