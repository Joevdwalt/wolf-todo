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

}
