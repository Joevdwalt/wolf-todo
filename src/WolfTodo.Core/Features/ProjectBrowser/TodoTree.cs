namespace WolfTodo.Core.Features.ProjectBrowser;

public static class TodoTree
{
    public static IEnumerable<TodoItem> Enumerate(IEnumerable<TodoItem> roots)
    {
        foreach (var todo in roots)
        {
            yield return todo;
            foreach (var child in Enumerate(todo.Subtasks))
            {
                yield return child;
            }
        }
    }

    public static TodoItem? FindBySourceLine(IEnumerable<TodoItem> roots, int sourceLine) =>
        Enumerate(roots).FirstOrDefault(todo => todo.SourceLine == sourceLine);

    public static TodoItem? FindBySourceLine(
        ProjectCatalog catalog,
        string projectPath,
        int sourceLine)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var project = catalog.Projects.FirstOrDefault(candidate =>
            string.Equals(candidate.Path, projectPath, comparison));
        return project is null ? null : FindBySourceLine(project.Todos, sourceLine);
    }
}
