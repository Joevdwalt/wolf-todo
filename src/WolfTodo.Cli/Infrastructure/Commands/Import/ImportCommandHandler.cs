using System.Text.Json;
using WolfTodo.Cli.Features;

namespace WolfTodo.Cli.Infrastructure.Commands.Import;

public sealed class ImportCommandHandler(
    TaskImportService importService,
    TaskUpdateFactory taskFactory,
    CliOutputWriter output,
    TextReader input,
    Func<string, string> readAllText)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public int Execute(ImportCommand command, IReadOnlyList<string> arguments)
    {
        if (arguments.Count(value => string.Equals(value, "--stdin", StringComparison.Ordinal)) > 1)
            throw new CommandException("duplicate_option", "Option --stdin may only be specified once.");

        var file = CommandOptionValues.OptionalSingle(command.File, "--file");
        if ((file is null) == !command.Stdin)
            throw new CommandException("invalid_input_source", "Specify exactly one of --file <path> or --stdin.");

        ImportDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ImportDocument>(
                command.Stdin ? input.ReadToEnd() : readAllText(file!), JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new CommandException("invalid_json", exception.Message);
        }

        if (document is null || string.IsNullOrWhiteSpace(document.Project))
            throw new CommandException("invalid_json", "JSON project must be a non-empty string.");
        if (document.Tasks is null || document.Tasks.Count == 0)
            throw new CommandException("invalid_json", "JSON tasks must contain at least one task.");

        var tasks = document.Tasks.Select((task, index) => task is null
            ? throw new CommandException("invalid_task", $"Task {index + 1} must be an object.")
            : taskFactory.FromTask(task, index + 1)).ToArray();
        var result = importService.Import(document.Project, tasks);
        if (!result.Succeeded)
            return output.Error(1, result.ErrorCode!, result.Error!);

        output.Write(new
        {
            ok = true,
            project = new { title = result.ProjectTitle, path = result.ProjectPath },
            created_count = result.SourceLines.Count,
            created = result.SourceLines.Select((line, index) => new { index, source_line = line })
        });
        return 0;
    }
}
