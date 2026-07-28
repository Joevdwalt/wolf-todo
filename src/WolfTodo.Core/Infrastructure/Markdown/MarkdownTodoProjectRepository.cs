using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Infrastructure.Markdown;

/// <summary>
/// File-backed read repository for one Markdown todo project. Markdown syntax
/// remains in <see cref="MarkdownTodoProjectReader"/> so it can be reused without I/O.
/// </summary>
public sealed class MarkdownTodoProjectRepository(
    IProjectFileSystem fileSystem,
    MarkdownTodoProjectReader reader) : ITodoProjectRepository
{
    public string CanonicalizePath(string path) => fileSystem.GetFullPath(path);

    public TodoProjectReadResult Read(string path)
    {
        var canonicalPath = CanonicalizePath(path);
        if (!fileSystem.FileExists(canonicalPath))
        {
            return TodoProjectReadResult.Failure(canonicalPath, $"Project file does not exist: {canonicalPath}");
        }

        try
        {
            var parsed = reader.Parse(canonicalPath, fileSystem.ReadAllText(canonicalPath));
            return parsed.Project is not null
                ? TodoProjectReadResult.Success(canonicalPath, parsed.Project)
                : TodoProjectReadResult.Failure(canonicalPath, parsed.Error ?? "Invalid project file.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return TodoProjectReadResult.Failure(canonicalPath, $"Cannot read project file: {exception.Message}");
        }
    }
}
