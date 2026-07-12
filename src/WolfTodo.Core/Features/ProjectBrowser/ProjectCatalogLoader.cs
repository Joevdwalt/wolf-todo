using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed class ProjectCatalogLoader(
    IProjectFileSystem fileSystem,
    ProjectMarkdownParser parser)
{
    public ProjectCatalog Load(IEnumerable<string> configuredFiles)
    {
        var projects = ImmutableArray.CreateBuilder<TodoProject>();
        var errors = ImmutableArray.CreateBuilder<ProjectSourceError>();
        var loadedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var configuredFile in configuredFiles)
        {
            var path = Canonicalize(configuredFile);

            if (!loadedPaths.Add(path))
            {
                continue;
            }

            if (!fileSystem.FileExists(path))
            {
                errors.Add(new ProjectSourceError(
                    System.IO.Path.GetFileName(path),
                    path,
                    $"Project file does not exist: {path}"));
                continue;
            }

            try
            {
                var result = parser.Parse(path, fileSystem.ReadAllText(path));

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
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(new ProjectSourceError(
                    System.IO.Path.GetFileNameWithoutExtension(path),
                    path,
                    $"Cannot read project file: {exception.Message}"));
            }
        }

        return new ProjectCatalog(
            [.. projects.OrderBy(project => project.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(project => project.Path, StringComparer.OrdinalIgnoreCase)],
            errors.ToImmutable());
    }

    private string Canonicalize(string path) => fileSystem.GetFullPath(path);

}
