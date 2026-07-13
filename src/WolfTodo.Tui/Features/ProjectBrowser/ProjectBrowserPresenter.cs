using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class ProjectBrowserPresenter
{
    public BrowserView CreateView(ProjectCatalog catalog, BrowserState state)
    {
        var filter = EffectiveFilter(state);
        var projectRows = BuildProjectRows(catalog, state);
        var selectedProjectIndex = Math.Clamp(state.ProjectIndex, 0, Math.Max(0, projectRows.Length - 1));
        var selectedProject = projectRows[selectedProjectIndex];
        var todoRows = BuildTodoRows(catalog, selectedProject, state);
        var selectableTodos = todoRows.Where(row => row.Todo is not null).ToArray();
        var selectedTodoIndex = Math.Clamp(state.TodoIndex, 0, Math.Max(0, selectableTodos.Length - 1));
        var selectedTodo = selectableTodos.Length == 0 ? null : selectableTodos[selectedTodoIndex].Todo;
        var markedRows = todoRows
            .Select(row => row.Todo is null
                ? row
                : row with { IsSelected = ReferenceEquals(row.Todo, selectedTodo) })
            .ToImmutableArray();

        var emptyMessage = catalog.Projects.Length == 0
            ? "No projects found"
            : filter.Length > 0
                ? $"No todos match /{filter}"
            : state.ShowCompleted
                ? "No todos in this view"
                : HasCompletedTodos(selectedProject, catalog)
                    ? "No active todos — use :completed to show completed todos"
                    : "No active todos";

        return new BrowserView(
            state with { ProjectIndex = selectedProjectIndex, TodoIndex = selectedTodoIndex },
            projectRows,
            markedRows,
            selectedTodo,
            selectedProject.Title,
            selectedProject.Project?.Path ?? selectedProject.Error?.Path,
            selectedProject.Error?.Message,
            emptyMessage);
    }

    private static ImmutableArray<ProjectRow> BuildProjectRows(ProjectCatalog catalog, BrowserState state)
    {
        var rows = new List<ProjectRow>
        {
            new(
                "All",
                catalog.Projects.Sum(project => CountActive(project.Todos)),
                null,
                null,
                state.ProjectIndex == 0)
        };

        rows.AddRange(catalog.Projects.Select((project, index) => new ProjectRow(
            project.Title,
            CountActive(project.Todos),
            project,
            null,
            state.ProjectIndex == index + 1)));

        rows.AddRange(catalog.Errors.Select((error, index) => new ProjectRow(
            error.DisplayName,
            0,
            null,
            error,
            state.ProjectIndex == catalog.Projects.Length + index + 1)));

        return [.. rows];
    }

    private static ImmutableArray<TodoRow> BuildTodoRows(
        ProjectCatalog catalog,
        ProjectRow selectedProject,
        BrowserState state)
    {
        if (selectedProject.Error is not null)
        {
            return [];
        }

        var projects = selectedProject.Project is null
            ? catalog.Projects
            : [selectedProject.Project];
        var rows = ImmutableArray.CreateBuilder<TodoRow>();
        var filter = EffectiveFilter(state);

        foreach (var project in projects)
        {
            var visibleTodos = Flatten(project.Todos)
                .Where(item => state.ShowCompleted || !item.Todo.IsCompleted)
                .Where(item => MatchesFilter(item.Todo, filter))
                .ToArray();

            if (filter.Length > 0 && visibleTodos.Length == 0)
            {
                continue;
            }

            if (selectedProject.Project is null)
            {
                rows.Add(new TodoRow(project.Title, null, 0, false));
            }

            var flattened = visibleTodos
                .GroupBy(item => item.Todo.SectionPath);

            foreach (var section in flattened)
            {
                if (!string.IsNullOrEmpty(section.Key))
                {
                    rows.Add(new TodoRow(section.Key, null, 0, false));
                }

                foreach (var item in section.OrderBy(item => item.Todo.IsCompleted))
                {
                    rows.Add(new TodoRow(null, item.Todo, item.Depth, false));
                }
            }
        }

        return rows.ToImmutable();
    }

    private static string EffectiveFilter(BrowserState state) =>
        (state.IsFilterMode ? state.FilterDraft : state.FilterText).Trim();

    private static bool MatchesFilter(TodoItem todo, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }

        return Contains(todo.Title, filter)
            || Contains(todo.ExternalReference, filter)
            || Contains(todo.SectionPath, filter)
            || todo.Tags.Any(tag => Contains(tag, filter) || Contains($"#{tag}", filter));
    }

    private static bool Contains(string? value, string filter) =>
        value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;

    private static IEnumerable<(TodoItem Todo, int Depth)> Flatten(
        IEnumerable<TodoItem> todos,
        int depth = 0)
    {
        foreach (var todo in todos)
        {
            yield return (todo, depth);

            foreach (var subtask in Flatten(todo.Subtasks, depth + 1))
            {
                yield return subtask;
            }
        }
    }

    private static int CountActive(IEnumerable<TodoItem> todos) =>
        Flatten(todos).Count(item => !item.Todo.IsCompleted);

    private static bool HasCompletedTodos(ProjectRow selectedProject, ProjectCatalog catalog)
    {
        var projects = selectedProject.Project is null
            ? catalog.Projects
            : [selectedProject.Project];

        return projects.SelectMany(project => Flatten(project.Todos)).Any(item => item.Todo.IsCompleted);
    }
}
