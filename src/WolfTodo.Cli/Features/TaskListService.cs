using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed class TaskListService(
    TomlProjectConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader)
{
    public TaskListResult List(string? projectTarget)
    {
        var catalog = catalogLoader.Load(configurationLoader.Load());
        if (projectTarget is null)
        {
            return TaskListResult.Success(catalog.Projects);
        }

        var resolution = ProjectTargetResolver.Resolve(catalog, projectTarget);
        return resolution.Error is null
            ? TaskListResult.Success([resolution.Project!])
            : TaskListResult.Failure(resolution.ErrorCode!, resolution.Error);
    }

    public TaskLookupResult Get(string code)
    {
        if (!TaskLinkCode.IsValid(code))
        {
            return TaskLookupResult.Failure("invalid_task_code", TaskLinkCode.InvalidCodeMessage);
        }

        var catalog = catalogLoader.Load(configurationLoader.Load());
        var match = TaskLinkCode.ResolveMatch(catalog, code, out var ambiguous);
        if (match is not null)
        {
            return TaskLookupResult.Success(match);
        }

        return ambiguous
            ? TaskLookupResult.Failure(
                "ambiguous_task_code",
                $"Task link code '{code}' matches multiple configured task locations.")
            : TaskLookupResult.Failure(
                "task_not_found",
                $"No configured task matches link code '{code}'.");
    }

}
