using System.Collections.Immutable;
using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed class TaskUpdateService(
    TomlProjectConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader,
    ProjectTodoMutationService mutationService)
{
    public TaskUpdateResult Update(string code, TaskUpdatePatch patch)
    {
        if (!patch.HasChanges)
        {
            return TaskUpdateResult.Failure(
                "missing_update",
                "Command update requires at least one field option.");
        }

        if (!TaskLinkCode.IsValid(code))
        {
            return TaskUpdateResult.Failure("invalid_task_code", TaskLinkCode.InvalidCodeMessage);
        }

        var configuredFiles = configurationLoader.Load();
        var catalog = catalogLoader.Load(configuredFiles);
        var match = TaskLinkCode.ResolveMatch(catalog, code, out var ambiguous);
        if (match is null)
        {
            return ambiguous
                ? TaskUpdateResult.Failure(
                    "ambiguous_task_code",
                    $"Task link code '{code}' matches multiple configured task locations.")
                : TaskUpdateResult.Failure(
                    "task_not_found",
                    $"No configured task matches link code '{code}'.");
        }

        var update = BuildUpdate(match.Todo, patch, out var desired);
        if (update.Error is not null)
        {
            return TaskUpdateResult.Failure(update.ErrorCode!, update.Error);
        }

        if (SameTask(match.Todo, desired))
        {
            return TaskUpdateResult.Success(match, changed: false);
        }

        if (desired.Schedule?.Time is { } time &&
            HasScheduleCollision(catalog, match, desired.Schedule))
        {
            return TaskUpdateResult.Failure(
                "schedule_conflict",
                $"The timed schedule {desired.Schedule.Date:yyyy-MM-dd} {time:HH:mm} is already occupied.");
        }

        var mutation = mutationService.UpdateTask(match.Project.Path, match.Todo, update.Value!);
        if (!mutation.Succeeded)
        {
            return TaskUpdateResult.Failure("mutation_failed", mutation.Error!);
        }

        var refreshedCatalog = catalogLoader.Load(configuredFiles);
        var refreshed = TaskLinkCode.ResolveMatch(refreshedCatalog, code, out var refreshedAmbiguous);
        if (refreshed is null)
        {
            return TaskUpdateResult.Failure(
                refreshedAmbiguous ? "ambiguous_task_code" : "task_not_found",
                refreshedAmbiguous
                    ? $"Task link code '{code}' matches multiple configured task locations after the update."
                    : $"No configured task matches link code '{code}' after the update.");
        }

        return TaskUpdateResult.Success(refreshed, changed: true);
    }

    private static TaskUpdateBuildResult BuildUpdate(
        TodoItem current,
        TaskUpdatePatch patch,
        out TodoItem desired)
    {
        var scheduleResult = ResolveSchedule(current.Schedule, patch, out var schedule);
        if (scheduleResult is not null)
        {
            desired = current;
            return TaskUpdateBuildResult.Failure("invalid_task", scheduleResult);
        }

        var content = patch.ClearContent
            ? string.Empty
            : patch.HasContent
                ? patch.Content ?? string.Empty
                : string.Join('\n', current.Notes.Select(note => note.Text));

        desired = current with
        {
            IsCompleted = patch.Completed ?? current.IsCompleted,
            Title = patch.HasTitle ? patch.Title! : current.Title,
            ExternalReference = patch.ClearReference
                ? null
                : patch.HasReference
                    ? NullIfWhiteSpace(patch.Reference)
                    : current.ExternalReference,
            Priority = patch.ClearPriority
                ? null
                : patch.HasPriority
                    ? patch.Priority
                    : current.Priority,
            Tags = patch.ClearTags
                ? []
                : patch.HasTags
                    ? patch.Tags
                    : current.Tags,
            Schedule = schedule,
            Duration = patch.ClearDuration
                ? null
                : patch.HasDuration
                    ? TimeSpan.FromMinutes(patch.DurationMinutes!.Value)
                    : current.Duration,
            Notes = patch.ClearContent || patch.HasContent
                ? content.Length == 0
                    ? []
                    : [new TodoNote(current.SourceLine + 1, content)]
                : current.Notes
        };

        ImmutableArray<TodoSubtaskUpdate> subtasks = [.. current.Subtasks.Select(subtask =>
            new TodoSubtaskUpdate(subtask.SourceLine, subtask.Title, subtask.IsCompleted))];

        return TaskUpdateBuildResult.Success(new TodoTaskUpdate(
            new TodoUpdate(
                desired.Title,
                desired.ExternalReference,
                desired.Priority,
                desired.Tags,
                desired.StartDate,
                desired.DueDate,
                desired.Schedule,
                desired.Duration),
            new TodoContentUpdate(content, subtasks),
            patch.Completed));
    }

    private static string? ResolveSchedule(
        TodoSchedule? current,
        TaskUpdatePatch patch,
        out TodoSchedule? schedule)
    {
        if (patch.ClearSchedule)
        {
            schedule = null;
            return null;
        }

        var date = patch.HasScheduledDate ? patch.ScheduledDate : current?.Date;
        var time = patch.HasTime ? patch.Time : patch.ClearTime ? null : current?.Time;
        if ((patch.HasTime || patch.ClearTime) && date is null)
        {
            schedule = current;
            return "A scheduled date is required before setting or clearing a scheduled time.";
        }

        schedule = date is null ? null : new TodoSchedule(date.Value, time);
        return null;
    }

    private static bool HasScheduleCollision(
        ProjectCatalog catalog,
        TaskLinkMatch target,
        TodoSchedule schedule) =>
        catalog.Projects
            .SelectMany(project => Flatten(project.Todos).Select(todo => (project, todo)))
            .Where(candidate =>
                !string.Equals(candidate.project.Path, target.Project.Path, StringComparison.OrdinalIgnoreCase) ||
                candidate.todo.SourceLine != target.Todo.SourceLine)
            .Any(candidate => candidate.todo.Schedule == schedule);

    private static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var child in Flatten(todo.Subtasks))
            {
                yield return child;
            }
        }
    }

    private static bool SameTask(TodoItem left, TodoItem right) =>
        left.IsCompleted == right.IsCompleted &&
        string.Equals(left.ExternalReference, right.ExternalReference, StringComparison.Ordinal) &&
        string.Equals(left.Title, right.Title, StringComparison.Ordinal) &&
        left.Priority == right.Priority &&
        left.Tags.SequenceEqual(right.Tags, StringComparer.Ordinal) &&
        left.StartDate == right.StartDate &&
        left.DueDate == right.DueDate &&
        left.Schedule == right.Schedule &&
        left.Duration == right.Duration &&
        string.Equals(
            string.Join('\n', left.Notes.Select(note => note.Text)),
            string.Join('\n', right.Notes.Select(note => note.Text)),
            StringComparison.Ordinal);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
