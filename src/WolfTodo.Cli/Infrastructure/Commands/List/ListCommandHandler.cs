using System.Globalization;
using WolfTodo.Cli.Features;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Infrastructure.Commands.List;

public sealed class ListCommandHandler(TaskListService listService, CliOutputWriter output)
{
    public int Execute(ListCommand command)
    {
        var project = CommandOptionValues.OptionalSingle(command.Project, "--project");
        var result = listService.List(project);
        if (!result.Succeeded)
            return output.Error(1, result.ErrorCode!, result.Error!);

        var tasks = result.Projects.SelectMany(project => Flatten(project.Todos).Select(entry => new
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
            schedule = entry.Todo.Schedule is { } schedule ? new
            {
                date = schedule.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                time = schedule.Time?.ToString("HH:mm", CultureInfo.InvariantCulture)
            } : null,
            duration_minutes = entry.Todo.Duration is { } duration ? (int?)duration.TotalMinutes : null,
            notes = entry.Todo.Notes.Select(note => note.Text)
        })).ToArray();

        output.Write(new { ok = true, task_count = tasks.Length, tasks });
        return 0;
    }

    private static IEnumerable<(TodoItem Todo, int? ParentSourceLine)> Flatten(
        IEnumerable<TodoItem> todos, int? parentSourceLine = null)
    {
        foreach (var todo in todos)
        {
            yield return (todo, parentSourceLine);
            foreach (var child in Flatten(todo.Subtasks, todo.SourceLine))
                yield return child;
        }
    }
}
