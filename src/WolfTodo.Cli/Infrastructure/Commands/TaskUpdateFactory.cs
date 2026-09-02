using System.Collections.Immutable;
using System.Globalization;
using WolfTodo.Cli.Infrastructure.Commands.Add;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class TaskUpdateFactory
{
    public TodoTaskUpdate FromAdd(AddCommand command, IReadOnlyList<string> arguments)
    {
        var project = CommandOptionValues.RequiredSingle(command.Project, "--project");
        var title = CommandOptionValues.RequiredSingle(command.Title, "--title");
        var reference = CommandOptionValues.OptionalSingle(command.Reference, "--reference");
        var priority = CommandOptionValues.OptionalSingle(command.Priority, "--priority");
        var scheduled = CommandOptionValues.OptionalSingle(command.Scheduled, "--scheduled");
        var time = CommandOptionValues.OptionalSingle(command.Time, "--time");
        var duration = CommandOptionValues.OptionalSingle(command.DurationMinutes, "--duration-minutes");
        var content = CommandOptionValues.OptionalSingle(command.Content, "--content");
        var subtasks = command.Subtasks
            .Select(title => new SubtaskInput { Title = title, Completed = false })
            .Concat(command.CompletedSubtasks.Select(title => new SubtaskInput { Title = title, Completed = true }))
            .ToArray();

        return FromTask(new TaskInput
        {
            Title = title,
            Reference = reference,
            Priority = priority,
            Tags = [.. command.Tags],
            Schedule = scheduled is null && time is null ? null : new ScheduleInput { Date = scheduled, Time = time },
            DurationMinutes = ParseOptionalInteger(duration, "--duration-minutes"),
            Content = content,
            Subtasks = [.. subtasks]
        }, null);
    }

    public TodoTaskUpdate FromTask(TaskInput task, int? taskNumber)
    {
        var prefix = taskNumber is null ? string.Empty : $"Task {taskNumber}: ";
        if (string.IsNullOrWhiteSpace(task.Title))
            throw new CommandException("invalid_task", prefix + "title must be a non-empty string.");
        if (task.Title.IndexOfAny(['\r', '\n']) >= 0)
            throw new CommandException("invalid_task", prefix + "title must stay on one line.");
        if (task.Reference?.IndexOfAny([')', '\r', '\n']) >= 0)
            throw new CommandException("invalid_task", prefix + "reference must not contain a closing parenthesis or line break.");

        TodoPriority? priority = null;
        if (task.Priority is not null)
        {
            var named = Enum.GetNames<TodoPriority>().FirstOrDefault(name =>
                string.Equals(name, task.Priority, StringComparison.OrdinalIgnoreCase));
            if (named is null)
                throw new CommandException("invalid_task", prefix + "priority must be lowest, low, medium, high, or highest.");
            priority = Enum.Parse<TodoPriority>(named);
        }

        TodoSchedule? schedule = null;
        if (task.Schedule is not null)
        {
            if (string.IsNullOrWhiteSpace(task.Schedule.Date))
                throw new CommandException("invalid_task", prefix + "schedule.date is required when schedule is set.");
            if (!DateOnly.TryParseExact(task.Schedule.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                throw new CommandException("invalid_task", prefix + "schedule.date must use YYYY-MM-DD.");

            TimeOnly? time = null;
            if (task.Schedule.Time is not null)
            {
                if (!TimeOnly.TryParseExact(task.Schedule.Time, "HH:mm", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var parsedTime))
                    throw new CommandException("invalid_task", prefix + "schedule.time must use HH:mm.");
                time = parsedTime;
            }
            if (time is { } scheduledTime &&
                (scheduledTime.Minute is not (0 or 15 or 30 or 45) ||
                 scheduledTime < new TimeOnly(6, 0) || scheduledTime > new TimeOnly(21, 45)))
                throw new CommandException("invalid_task", prefix + "schedule.time must be a quarter-hour from 06:00 through 21:45.");
            schedule = new TodoSchedule(date, time);
        }

        if (task.DurationMinutes is { } durationMinutes &&
            (durationMinutes is < 15 or > 960 || durationMinutes % 15 != 0))
            throw new CommandException("invalid_task", prefix + "duration_minutes must be a 15-minute value from 15 through 960.");

        var tags = (task.Tags ?? []).Select(tag => tag?.Trim().TrimStart('#') ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();
        if (tags.Any(tag => tag.Length == 0 || tag.Any(char.IsWhiteSpace)))
            throw new CommandException("invalid_task", prefix + "tags must be non-empty hashtags without whitespace.");

        var content = task.Content ?? string.Empty;
        if (content.Contains('\r'))
            content = content.Replace("\r\n", "\n").Replace('\r', '\n');

        var subtasks = (task.Subtasks ?? []).Select((item, index) => item is null
            ? throw new CommandException("invalid_task", $"{prefix}subtask {index + 1} must be an object.")
            : BuildSubtask(item, prefix, index + 1)).ToImmutableArray();

        return new TodoTaskUpdate(
            new TodoUpdate(task.Title, task.Reference, priority, tags, null, null, schedule,
                task.DurationMinutes is null ? null : TimeSpan.FromMinutes(task.DurationMinutes.Value)),
            new TodoContentUpdate(content, [.. subtasks]));
    }

    private static TodoSubtaskUpdate BuildSubtask(SubtaskInput item, string prefix, int index)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
            throw new CommandException("invalid_task", $"{prefix}subtask {index} title must be a non-empty string.");
        if (item.Title.IndexOfAny(['\r', '\n']) >= 0)
            throw new CommandException("invalid_task", $"{prefix}subtask {index} title must stay on one line.");
        return new TodoSubtaskUpdate(null, item.Title.Trim(), item.Completed ?? false);
    }

    private static int? ParseOptionalInteger(string? value, string option)
    {
        if (value is null) return null;
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            throw new CommandException("invalid_option", $"Option {option} must be an integer.");
        return parsed;
    }
}
