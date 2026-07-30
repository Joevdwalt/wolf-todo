using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public static class TodoCatalogLookup
{
    public static TodoItem? Find(ProjectCatalog catalog, TodoIdentity? identity)
    {
        if (identity is null)
        {
            return null;
        }

        var project = catalog.Projects.FirstOrDefault(candidate => candidate.Path == identity.ProjectPath);
        return project is null ? null : Find(project.Todos, identity.SourceLine);
    }

    public static TodoItem? Find(IEnumerable<TodoItem> todos, int sourceLine) =>
        Flatten(todos).FirstOrDefault(todo => todo.SourceLine == sourceLine);

    public static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var subtask in Flatten(todo.Subtasks))
            {
                yield return subtask;
            }
        }
    }
}
