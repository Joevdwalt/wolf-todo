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

        var tasks = result.Projects
            .SelectMany(project => Flatten(project.Todos)
                .Select(entry => TaskListEntryFactory.Create(project, entry.Todo, entry.ParentSourceLine)))
            .ToArray();

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
