using WolfTodo.Cli.Infrastructure.Commands;

namespace WolfTodo.Cli.Infrastructure;

public class ArgumentResolver
{
    public static AddOptions ParseAdd(string[] args)
    {
        var result = new AddOptions();
        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            switch (option)
            {
                case "--project":
                    result.Project = ReadUniqueValue(args, ref index, result.Project, option);
                    break;
                case "--title":
                    result.Title = ReadUniqueValue(args, ref index, result.Title, option);
                    break;
                case "--reference":
                    result.Reference = ReadUniqueValue(args, ref index, result.Reference, option);
                    break;
                case "--priority":
                    result.Priority = ReadUniqueValue(args, ref index, result.Priority, option);
                    break;
                case "--tag":
                    result.Tags.Add(ReadValue(args, ref index, option));
                    break;
                case "--scheduled":
                    result.Scheduled = ReadUniqueValue(args, ref index, result.Scheduled, option);
                    break;
                case "--time":
                    result.Time = ReadUniqueValue(args, ref index, result.Time, option);
                    break;
                case "--duration-minutes":
                    result.DurationMinutes = ReadUniqueValue(args, ref index, result.DurationMinutes, option);
                    break;
                case "--note":
                    result.Content.Add(new ContentInput { Type = "note", Text = ReadValue(args, ref index, option) });
                    break;
                case "--subtask":
                    result.Content.Add(new ContentInput
                    {
                        Type = "subtask",
                        Title = ReadValue(args, ref index, option),
                        Completed = false
                    });
                    break;
                case "--completed-subtask":
                    result.Content.Add(new ContentInput
                    {
                        Type = "subtask",
                        Title = ReadValue(args, ref index, option),
                        Completed = true
                    });
                    break;
                default:
                    throw new CommandException("unknown_option", $"Unknown add option '{option}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(result.Project))
        {
            throw new CommandException("missing_option", "Option --project is required.");
        }

        if (string.IsNullOrWhiteSpace(result.Title))
        {
            throw new CommandException("missing_option", "Option --title is required.");
        }

        return result;
    }
    
    
    public static string ReadUniqueValue(
        string[] args,
        ref int index,
        string? current,
        string option)
    {
        if (current is not null)
        {
            throw new CommandException("duplicate_option", $"Option {option} may only be specified once.");
        }

        return ReadValue(args, ref index, option);
    }
    
    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new CommandException("missing_value", $"Option {option} requires a value.");
        }

        index++;
        return args[index];
    }
}