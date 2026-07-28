using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed class ProjectCatalogLoader(ITodoProjectRepository repository)
{
    public ProjectCatalog Load(IEnumerable<string> configuredFiles)
    {
        var projects = ImmutableArray.CreateBuilder<TodoProject>();
        var errors = ImmutableArray.CreateBuilder<ProjectSourceError>();
        var loadedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var configuredFile in configuredFiles)
        {
            var path = repository.CanonicalizePath(configuredFile);

            if (!loadedPaths.Add(path))
            {
                continue;
            }

            var result = repository.Read(path);
            if (result.Project is not null)
            {
                projects.Add(result.Project);
            }
            else
            {
                errors.Add(new ProjectSourceError(
                    System.IO.Path.GetFileNameWithoutExtension(path),
                    path,
                    result.Error ?? "Invalid project file."));
            }
        }

        return new ProjectCatalog(
            [.. projects.OrderBy(project => project.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(project => project.Path, StringComparer.OrdinalIgnoreCase)],
            errors.ToImmutable());
    }
}
