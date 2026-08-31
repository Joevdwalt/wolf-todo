using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed class TaskListService(
    TomlProjectConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader)
{
    public Result List(string? projectTarget)
    {
        var catalog = catalogLoader.Load(configurationLoader.Load());
        if (projectTarget is null)
        {
            return Result.Success(catalog.Projects);
        }

        var resolution = ProjectTargetResolver.Resolve(catalog, projectTarget);
        return resolution.Error is null
            ? Result.Success([resolution.Project!])
            : Result.Failure(resolution.ErrorCode!, resolution.Error);
    }

    public sealed record Result(
        bool Succeeded,
        string? ErrorCode,
        string? Error,
        IReadOnlyList<TodoProject> Projects)
    {
        public static Result Success(IReadOnlyList<TodoProject> projects) => new(true, null, null, projects);

        public static Result Failure(string code, string error) => new(false, code, error, []);
    }
}
