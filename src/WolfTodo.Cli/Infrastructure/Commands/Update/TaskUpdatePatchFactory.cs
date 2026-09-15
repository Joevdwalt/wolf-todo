using System.Collections.Immutable;
using System.Globalization;
using WolfTodo.Cli.Features;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Infrastructure.Commands.Update;

public sealed class TaskUpdatePatchFactory
{
    public TaskUpdatePatch Create(UpdateCommand command)
    {
        var completed = ParseCompleted(command.Completed);
        var title = CommandOptionValues.OptionalSingle(command.Title, "--title");
        var reference = CommandOptionValues.OptionalSingle(command.Reference, "--reference");
        var priority = ParsePriority(CommandOptionValues.OptionalSingle(command.Priority, "--priority"));
        var scheduled = ParseDate(CommandOptionValues.OptionalSingle(command.Scheduled, "--scheduled"));
        var time = ParseTime(CommandOptionValues.OptionalSingle(command.Time, "--time"));
        var durationText = CommandOptionValues.OptionalSingle(command.DurationMinutes, "--duration-minutes");
        var duration = ParseDuration(durationText);
        var content = CommandOptionValues.OptionalSingle(command.Content, "--content");

        if (title is not null && (string.IsNullOrWhiteSpace(title) || title.IndexOfAny(['\r', '\n']) >= 0))
            throw new CommandException("invalid_task", "Option --title must be a non-empty single-line string.");
        if (reference?.IndexOfAny([')', '\r', '\n']) >= 0)
            throw new CommandException("invalid_task", "Option --reference must not contain a closing parenthesis or line break.");

        if (command.ClearReference && reference is not null)
            throw new CommandException("conflicting_options", "Use either --reference or --clear-reference, not both.");
        if (command.ClearPriority && priority is not null)
            throw new CommandException("conflicting_options", "Use either --priority or --clear-priority, not both.");
        if (command.ClearTags && command.Tags.Length > 0)
            throw new CommandException("conflicting_options", "Use either --tag or --clear-tags, not both.");
        if (command.ClearTime && time is not null)
            throw new CommandException("conflicting_options", "Use either --time or --clear-time, not both.");
        if (command.ClearSchedule && (scheduled is not null || time is not null || command.ClearTime))
            throw new CommandException("conflicting_options", "--clear-schedule cannot be combined with schedule options.");
        if (command.ClearDuration && duration is not null)
            throw new CommandException("conflicting_options", "Use either --duration-minutes or --clear-duration, not both.");
        if (command.ClearContent && content is not null)
            throw new CommandException("conflicting_options", "Use either --content or --clear-content, not both.");

        var tags = command.Tags
            .Select(tag => tag.Trim().TrimStart('#'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        if (tags.Any(tag => tag.Length == 0 || tag.Any(char.IsWhiteSpace)))
            throw new CommandException("invalid_task", "Tags must be non-empty hashtags without whitespace.");

        var patch = new TaskUpdatePatch
        {
            Completed = completed,
            HasTitle = title is not null,
            Title = title,
            HasReference = reference is not null,
            Reference = reference,
            ClearReference = command.ClearReference,
            HasPriority = priority is not null,
            Priority = priority,
            ClearPriority = command.ClearPriority,
            HasTags = command.Tags.Length > 0,
            Tags = tags,
            ClearTags = command.ClearTags,
            HasScheduledDate = scheduled is not null,
            ScheduledDate = scheduled,
            HasTime = time is not null,
            Time = time,
            ClearTime = command.ClearTime,
            ClearSchedule = command.ClearSchedule,
            HasDuration = duration is not null,
            DurationMinutes = duration,
            ClearDuration = command.ClearDuration,
            HasContent = content is not null,
            Content = content,
            ClearContent = command.ClearContent
        };

        if (!patch.HasChanges)
            throw new CommandException("missing_update", "Command update requires at least one field option.");

        return patch;
    }

    private static bool? ParseCompleted(string[] values)
    {
        var value = CommandOptionValues.OptionalSingle(values, "--completed");
        if (value is null) return null;
        if (!bool.TryParse(value, out var completed))
            throw new CommandException("invalid_task", "Option --completed must be true or false.");
        return completed;
    }

    private static TodoPriority? ParsePriority(string? value)
    {
        if (value is null) return null;
        var named = Enum.GetNames<TodoPriority>().FirstOrDefault(name =>
            string.Equals(name, value, StringComparison.OrdinalIgnoreCase));
        if (named is null)
            throw new CommandException("invalid_task", "Option --priority must be lowest, low, medium, high, or highest.");
        return Enum.Parse<TodoPriority>(named);
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (value is null) return null;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            throw new CommandException("invalid_task", "Option --scheduled must use YYYY-MM-DD.");
        return date;
    }

    private static TimeOnly? ParseTime(string? value)
    {
        if (value is null) return null;
        if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time) ||
            time.Minute is not (0 or 15 or 30 or 45) ||
            time < new TimeOnly(6, 0) || time > new TimeOnly(21, 45))
            throw new CommandException("invalid_task", "Option --time must be a quarter-hour from 06:00 through 21:45.");
        return time;
    }

    private static int? ParseDuration(string? value)
    {
        if (value is null) return null;
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var duration) ||
            duration is < 15 or > 960 || duration % 15 != 0)
            throw new CommandException("invalid_task", "Option --duration-minutes must be a 15-minute value from 15 through 960.");
        return duration;
    }
}
