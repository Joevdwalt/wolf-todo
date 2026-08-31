using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public static class ProjectTargetResolver
{
    public static Result Resolve(ProjectCatalog catalog, string target)
    {
        if (Path.IsPathFullyQualified(target))
        {
            var fullPath = Path.GetFullPath(target);
            var project = catalog.Projects.FirstOrDefault(candidate =>
                string.Equals(candidate.Path, fullPath, StringComparison.OrdinalIgnoreCase));
            if (project is not null)
            {
                return Result.Success(project);
            }

            var sourceError = catalog.Errors.FirstOrDefault(candidate =>
                string.Equals(candidate.Path, fullPath, StringComparison.OrdinalIgnoreCase));
            return sourceError is not null
                ? Result.Failure("invalid_project", sourceError.Message)
                : Result.Failure("project_not_configured", "The project path is not configured.");
        }

        var matches = catalog.Projects
            .Where(project => string.Equals(project.Title, target, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return matches.Length switch
        {
            1 => Result.Success(matches[0]),
            > 1 => Result.Failure(
                "ambiguous_project",
                $"Project title '{target}' is ambiguous; use an absolute configured path."),
            _ => Result.Failure("project_not_found", $"Configured project '{target}' was not found.")
        };
    }

    public sealed record Result(TodoProject? Project, string? ErrorCode, string? Error)
    {
        public static Result Success(TodoProject project) => new(project, null, null);

        public static Result Failure(string code, string error) => new(null, code, error);
    }
}
