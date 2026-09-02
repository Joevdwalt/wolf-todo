using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using WolfTodo.Cli.Features;
using WolfTodo.Cli.Infrastructure;
using WolfTodo.Cli.Infrastructure.Commands;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli;

public sealed class CliApplication(
    TaskImportService importService,
    TaskListService listService,
    TextReader input,
    TextWriter output,
    Func<string, string> readAllText)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public int Run(string[] args)
    {
        try
        {
            if (ShouldRunHelp(args, out var run))
            {
                return run;
            }

            if (!Enum.TryParse <CommandsEnum>(args[0],ignoreCase: true, out var command))
            {
                WriteError(2, "unknown_command", $"Unknown command '{args[0]}'.");
            }
            
            return command switch
            {
                CommandsEnum.Add => RunAdd(args[1..]),
                CommandsEnum.Import => RunImport(args[1..]),
                CommandsEnum.List => RunList(args[1..]),
                _ => WriteError(2, "unknown_command", $"Unknown command '{args[0]}'.")
            };

        }
        catch (CommandException exception)
        {
            return WriteError(2, exception.Code, exception.Message);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return WriteError(1, "operation_failed", exception.Message);
        }
    }

    public bool ShouldRunHelp(string[] args, out int run)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            output.WriteLine(HelpText);
            run = 0;
            return true;
        }
        run = 1;
        return false;
    }

    private int RunAdd(string[] args)
    {
        var parsed = ArgumentResolver.ParseAdd(args);
        return Execute(parsed.Project!, [BuildTask(parsed)]);
    }

    private int RunImport(string[] args)
    {
        string? file = null;
        var stdin = false;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--file":
                    file = ArgumentResolver.ReadUniqueValue(args, ref index, file, "--file");
                    break;
                case "--stdin":
                    if (stdin)
                    {
                        throw new CommandException("duplicate_option", "Option --stdin may only be specified once.");
                    }

                    stdin = true;
                    break;
                default:
                    throw new CommandException("unknown_option", $"Unknown import option '{args[index]}'.");
            }
        }

        if ((file is null) == !stdin)
        {
            throw new CommandException(
                "invalid_input_source",
                "Specify exactly one of --file <path> or --stdin.");
        }

        var json = stdin ? input.ReadToEnd() : readAllText(file!);
        ImportDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ImportDocument>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new CommandException("invalid_json", exception.Message);
        }

        if (document is null || string.IsNullOrWhiteSpace(document.Project))
        {
            throw new CommandException("invalid_json", "JSON project must be a non-empty string.");
        }

        if (document.Tasks is null || document.Tasks.Count == 0)
        {
            throw new CommandException("invalid_json", "JSON tasks must contain at least one task.");
        }

        var tasks = document.Tasks.Select((task, index) => task is null
            ? throw new CommandException("invalid_task", $"Task {index + 1} must be an object.")
            : BuildTask(task, index + 1)).ToArray();
        return Execute(document.Project, tasks);
    }

    private int RunList(string[] args)
    {
        string? project = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--project":
                    project = ArgumentResolver.ReadUniqueValue(args, ref index, project, "--project");
                    break;
                default:
                    throw new CommandException("unknown_option", $"Unknown list option '{args[index]}'.");
            }
        }

        var result = listService.List(project);
        if (!result.Succeeded)
        {
            return WriteError(1, result.ErrorCode!, result.Error!);
        }

        var tasks = result.Projects.SelectMany(project => Flatten(project.Todos)
                .Select(entry => new
                {
                    project = new { title = project.Title, path = project.Path },
                    source_line = entry.Todo.SourceLine,
                    parent_source_line = entry.ParentSourceLine,
                    completed = entry.Todo.IsCompleted,
                    reference = entry.Todo.ExternalReference,
                    title = entry.Todo.Title,
                    priority = entry.Todo.Priority?.ToString().ToLowerInvariant(),
                    tags = entry.Todo.Tags,
                    section_path = entry.Todo.SectionPath,
                    schedule = entry.Todo.Schedule is { } schedule
                        ? new
                        {
                            date = schedule.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            time = schedule.Time?.ToString("HH:mm", CultureInfo.InvariantCulture)
                        }
                        : null,
                    duration_minutes = entry.Todo.Duration is { } duration
                        ? (int?)duration.TotalMinutes
                        : null,
                    notes = entry.Todo.Notes.Select(note => note.Text)
                }))
            .ToArray();
        WriteJson(new { ok = true, task_count = tasks.Length, tasks });
        return 0;
    }

    private int Execute(string project, IReadOnlyList<TodoTaskUpdate> tasks)
    {
        var result = importService.Import(project, tasks);
        if (!result.Succeeded)
        {
            return WriteError(1, result.ErrorCode!, result.Error!);
        }

        WriteJson(new
        {
            ok = true,
            project = new { title = result.ProjectTitle, path = result.ProjectPath },
            created_count = result.SourceLines.Count,
            created = result.SourceLines.Select((line, index) => new { index, source_line = line })
        });
        return 0;
    }

    private static IEnumerable<(TodoItem Todo, int? ParentSourceLine)> Flatten(
        IEnumerable<TodoItem> todos,
        int? parentSourceLine = null)
    {
        foreach (var todo in todos)
        {
            yield return (todo, parentSourceLine);
            foreach (var child in Flatten(todo.Subtasks, todo.SourceLine))
            {
                yield return child;
            }
        }
    }



    private static TodoTaskUpdate BuildTask(AddOptions options) => BuildTask(new TaskInput
    {
        Title = options.Title,
        Reference = options.Reference,
        Priority = options.Priority,
        Tags = [.. options.Tags],
        Schedule = options.Scheduled is null && options.Time is null
            ? null
            : new ScheduleInput { Date = options.Scheduled, Time = options.Time },
        DurationMinutes = ParseOptionalInteger(options.DurationMinutes, "--duration-minutes"),
        Content = [.. options.Content]
    }, null);

    private static TodoTaskUpdate BuildTask(TaskInput task, int? taskNumber)
    {
        var prefix = taskNumber is null ? string.Empty : $"Task {taskNumber}: ";
        if (string.IsNullOrWhiteSpace(task.Title))
        {
            throw new CommandException("invalid_task", prefix + "title must be a non-empty string.");
        }

        if (task.Title.IndexOfAny(['\r', '\n']) >= 0)
        {
            throw new CommandException("invalid_task", prefix + "title must stay on one line.");
        }

        if (task.Reference?.IndexOfAny([')', '\r', '\n']) >= 0)
        {
            throw new CommandException(
                "invalid_task",
                prefix + "reference must not contain a closing parenthesis or line break.");
        }

        TodoPriority? priority = null;
        if (task.Priority is not null)
        {
            var namedPriority = Enum.GetNames<TodoPriority>()
                .FirstOrDefault(name => string.Equals(name, task.Priority, StringComparison.OrdinalIgnoreCase));
            if (namedPriority is null)
            {
                throw new CommandException(
                    "invalid_task",
                    prefix + "priority must be lowest, low, medium, high, or highest.");
            }

            priority = Enum.Parse<TodoPriority>(namedPriority);
        }

        TodoSchedule? schedule = null;
        if (task.Schedule is not null)
        {
            if (string.IsNullOrWhiteSpace(task.Schedule.Date))
            {
                throw new CommandException("invalid_task", prefix + "schedule.date is required when schedule is set.");
            }

            if (!DateOnly.TryParseExact(
                    task.Schedule.Date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                throw new CommandException("invalid_task", prefix + "schedule.date must use YYYY-MM-DD.");
            }

            TimeOnly? time = null;
            if (task.Schedule.Time is not null)
            {
                if (!TimeOnly.TryParseExact(
                        task.Schedule.Time,
                        "HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedTime))
                {
                    throw new CommandException("invalid_task", prefix + "schedule.time must use HH:mm.");
                }

                time = parsedTime;
            }

            if (time is { } scheduledTime &&
                (scheduledTime.Minute is not (0 or 15 or 30 or 45) ||
                 scheduledTime < new TimeOnly(6, 0) ||
                 scheduledTime > new TimeOnly(21, 45)))
            {
                throw new CommandException(
                    "invalid_task",
                    prefix + "schedule.time must be a quarter-hour from 06:00 through 21:45.");
            }

            schedule = new TodoSchedule(date, time);
        }

        if (task.DurationMinutes is { } durationMinutes &&
            (durationMinutes is < 15 or > 960 || durationMinutes % 15 != 0))
        {
            throw new CommandException(
                "invalid_task",
                prefix + "duration_minutes must be a 15-minute value from 15 through 960.");
        }

        TimeSpan? duration = task.DurationMinutes is null
            ? null
            : TimeSpan.FromMinutes(task.DurationMinutes.Value);
        var tags = (task.Tags ?? [])
            .Select(tag => tag?.Trim().TrimStart('#') ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        if (tags.Any(tag => tag.Length == 0 || tag.Any(char.IsWhiteSpace)))
        {
            throw new CommandException(
                "invalid_task",
                prefix + "tags must be non-empty hashtags without whitespace.");
        }

        var content = (task.Content ?? []).Select((item, index) => item is null
            ? throw new CommandException(
                "invalid_task",
                $"{prefix}content item {index + 1} must be an object.")
            : BuildContent(item, prefix, index + 1)).ToImmutableArray();

        return new TodoTaskUpdate(
            new TodoUpdate(
                task.Title,
                task.Reference,
                priority,
                tags,
                null,
                null,
                schedule,
                duration),
            new TodoContentUpdate(content));
    }

    private static TodoContentItemUpdate BuildContent(ContentInput item, string prefix, int index) => item.Type switch
    {
        "note" when !string.IsNullOrWhiteSpace(item.Text) && item.Title is null && item.Completed is null =>
            new TodoNoteUpdate(null, item.Text),
        "subtask" when !string.IsNullOrWhiteSpace(item.Title) &&
                       item.Title.IndexOfAny(['\r', '\n']) < 0 &&
                       item.Text is null =>
            new TodoSubtaskUpdate(null, item.Title, item.Completed ?? false),
        "note" => throw new CommandException(
            "invalid_task",
            $"{prefix}content item {index} must contain text and only text."),
        "subtask" => throw new CommandException(
            "invalid_task",
            $"{prefix}content item {index} must contain title and optional completed."),
        _ => throw new CommandException(
            "invalid_task",
            $"{prefix}content item {index} type must be note or subtask.")
    };

    private static int? ParseOptionalInteger(string? value, string option)
    {
        if (value is null)
        {
            return null;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new CommandException("invalid_option", $"Option {option} must be an integer.");
        }

        return parsed;
    }





    private int WriteError(int exitCode, string code, string message)
    {
        WriteJson(new { ok = false, error = new { code, message } });
        return exitCode;
    }

    private void WriteJson(object value) => output.WriteLine(JsonSerializer.Serialize(value, JsonOptions));

    private const string HelpText = """
                                    Wolf Todo CLI

                                    wtodo add --project <title|absolute-path> --title <text> [options]
                                    wtodo import --file <path>
                                    wtodo import --stdin
                                    wtodo list [--project <title|absolute-path>]

                                    Add options:
                                      --reference <text>
                                      --priority <lowest|low|medium|high|highest>
                                      --tag <tag>                         Repeatable
                                      --scheduled <YYYY-MM-DD>
                                      --time <HH:mm>
                                      --duration-minutes <minutes>
                                      --note <text>                       Repeatable and ordered
                                      --subtask <title>                   Repeatable and ordered
                                      --completed-subtask <title>         Repeatable and ordered
                                    """;



    private sealed class ImportDocument
    {
        public string? Project { get; init; }
        public List<TaskInput?>? Tasks { get; init; }
    }

    private sealed class TaskInput
    {
        public string? Title { get; init; }
        public string? Reference { get; init; }
        public string? Priority { get; init; }
        public List<string?>? Tags { get; init; }
        public ScheduleInput? Schedule { get; init; }
        public int? DurationMinutes { get; init; }
        public List<ContentInput?>? Content { get; init; }
    }

    private sealed class ScheduleInput
    {
        public string? Date { get; init; }
        public string? Time { get; init; }
    }
}



    
