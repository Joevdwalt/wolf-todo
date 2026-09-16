using System.Globalization;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public static class TaskListEntryFactory
{
    public static TaskListEntry Create(TodoProject project, TodoItem todo, int? parentSourceLine)
    {
        var schedule = todo.Schedule is { } value
            ? new TaskScheduleInfo(
                value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                value.Time?.ToString("HH:mm", CultureInfo.InvariantCulture))
            : null;

        return new TaskListEntry(
            new TaskProjectInfo(project.Title, project.Path),
            todo.SourceLine,
            TaskLinkCode.Generate(project.Path, todo.SourceLine),
            parentSourceLine,
            todo.IsCompleted,
            todo.ExternalReference,
            todo.Title,
            todo.Priority?.ToString().ToLowerInvariant(),
            todo.Tags,
            todo.SectionPath,
            schedule,
            todo.Duration is { } duration ? (int)duration.TotalMinutes : null,
            todo.Notes.Select(note => note.Text).ToArray());
    }
}
