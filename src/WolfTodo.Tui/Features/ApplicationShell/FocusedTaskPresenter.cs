using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class FocusedTaskPresenter
{
    public FocusedTaskView? CreateView(ProjectCatalog catalog, FocusedTaskState state)
    {
        var project = catalog.Projects.FirstOrDefault(candidate => candidate.Path == state.RootIdentity.ProjectPath);
        var root = project is null
            ? null
            : Flatten(project.Todos).FirstOrDefault(item => item.Todo.SourceLine == state.RootIdentity.SourceLine).Todo;
        if (project is null || root is null || !MatchesSnapshot(root, state.RootSnapshot))
        {
            return null;
        }

        var items = Flatten([root], project.Path).ToImmutableArray();
        var selectedIdentity = items.Any(item => item.Identity == state.SelectedIdentity)
            ? state.SelectedIdentity
            : state.RootIdentity;
        items = [.. items.Select(item => item with { IsSelected = item.Identity == selectedIdentity })];
        var resolvedState = state with
        {
            RootSnapshot = root,
            SelectedIdentity = selectedIdentity
        };
        var projects = catalog.Projects
            .Select(candidate => new TodoEditorProjectOption(candidate.Title, candidate.Path))
            .ToImmutableArray();
        return new FocusedTaskView(resolvedState, project.Title, items, projects);
    }

    private static bool MatchesSnapshot(TodoItem current, TodoItem snapshot) =>
        snapshot.ExternalReference is { Length: > 0 }
            ? string.Equals(current.ExternalReference, snapshot.ExternalReference, StringComparison.Ordinal)
            : string.Equals(current.Title, snapshot.Title, StringComparison.Ordinal);

    private static IEnumerable<(TodoItem Todo, ImmutableArray<TodoTreeSegment> Path)> Flatten(
        IEnumerable<TodoItem> todos,
        ImmutableArray<TodoTreeSegment> path = default)
    {
        var values = todos.ToArray();
        for (var index = 0; index < values.Length; index++)
        {
            var todo = values[index];
            var itemPath = path.IsDefault
                ? ImmutableArray<TodoTreeSegment>.Empty
                : path.Add(index < values.Length - 1
                    ? TodoTreeSegment.HasFollowingSibling
                    : TodoTreeSegment.LastSibling);
            yield return (todo, itemPath);
            foreach (var child in Flatten(todo.Subtasks, itemPath))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<FocusedTaskItem> Flatten(
        IEnumerable<TodoItem> todos,
        string projectPath,
        ImmutableArray<TodoTreeSegment> path = default)
    {
        foreach (var item in Flatten(todos, path))
        {
            yield return new FocusedTaskItem(
                new TodoIdentity(projectPath, item.Todo.SourceLine),
                item.Todo,
                item.Path,
                false);
        }
    }
}
