using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed class TaskImportService(
    TomlProjectConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader,
    ProjectTodoMutationService mutationService)
{
    public Result Import(string projectTarget, IReadOnlyList<TodoTaskUpdate> tasks)
    {
        var catalog = catalogLoader.Load(configurationLoader.Load());
        var resolution = ProjectTargetResolver.Resolve(catalog, projectTarget);
        if (resolution.Error is not null)
        {
            return Result.Failure(resolution.ErrorCode!, resolution.Error);
        }

        var project = resolution.Project!;
        var collision = FindScheduleCollision(catalog, tasks);
        if (collision is not null)
        {
            return Result.Failure("schedule_conflict", collision);
        }

        var mutation = mutationService.CreateMany(project.Path, tasks);
        return mutation.Succeeded
            ? Result.Success(project, mutation.SourceLines)
            : Result.Failure("mutation_failed", mutation.Error!);
    }

    private static string? FindScheduleCollision(
        ProjectCatalog catalog,
        IReadOnlyList<TodoTaskUpdate> tasks)
    {
        var occupied = catalog.Projects
            .SelectMany(project => Flatten(project.Todos))
            .Where(todo => todo.Schedule?.Time is not null)
            .Select(todo => todo.Schedule!)
            .ToHashSet();

        foreach (var candidate in tasks.Select(task => task.Fields.Schedule)
                     .Where(schedule => schedule?.Time is not null)
                     .Cast<TodoSchedule>())
        {
            if (!occupied.Add(candidate))
            {
                return $"The timed schedule {candidate.Date:yyyy-MM-dd} {candidate.Time:HH:mm} is already occupied.";
            }
        }

        return null;
    }

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

    public sealed record Result(
        bool Succeeded,
        string? ErrorCode,
        string? Error,
        string? ProjectTitle,
        string? ProjectPath,
        IReadOnlyList<int> SourceLines)
    {
        public static Result Success(TodoProject project, IReadOnlyList<int> sourceLines) =>
            new(true, null, null, project.Title, project.Path, sourceLines);

        public static Result Failure(string code, string error) =>
            new(false, code, error, null, null, []);
    }
}
