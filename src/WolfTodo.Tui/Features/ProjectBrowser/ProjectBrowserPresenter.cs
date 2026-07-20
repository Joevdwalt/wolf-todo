using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class ProjectBrowserPresenter(Func<DateOnly>? todayProvider = null)
{
    private readonly Func<DateOnly> todayProvider = todayProvider ??
        (() => DateOnly.FromDateTime(DateTime.Today));

    public BrowserView CreateView(ProjectCatalog catalog, BrowserState state)
    {
        var today = todayProvider();
        var filter = EffectiveFilter(state);
        var projectRows = BuildProjectRows(catalog, state, today);
        var selectedProjectIndex = Math.Clamp(state.ProjectIndex, 0, Math.Max(0, projectRows.Length - 1));
        var selectedProject = projectRows[selectedProjectIndex];
        var todoRows = BuildTodoRows(catalog, selectedProject, state, today);
        var selectableTodos = todoRows.Where(row => row.Todo is not null).ToArray();
        var pendingIndex = state.PendingTodoSelection is null
            ? -1
            : Array.FindIndex(selectableTodos, row => row.Identity == state.PendingTodoSelection);
        var selectedTodoIndex = state.PendingTodoSelection is null
            ? Math.Clamp(state.TodoIndex, 0, Math.Max(0, selectableTodos.Length - 1))
            : Math.Max(0, pendingIndex);
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
            : selectedProject.Kind == ProjectRowKind.Today
                ? state.ShowCompleted
                    ? "No todos scheduled today"
                    : HasCompletedTodos(selectedProject, catalog, today)
                        ? "No active todos scheduled today — use :completed to show completed todos"
                        : "No active todos scheduled today"
            : state.ShowCompleted
                ? "No todos in this view"
                : HasCompletedTodos(selectedProject, catalog, today)
                    ? "No active todos — use :completed to show completed todos"
                    : "No active todos";

        return new BrowserView(
            state with
            {
                ProjectIndex = selectedProjectIndex,
                TodoIndex = selectedTodoIndex,
                PendingTodoSelection = null
            },
            projectRows,
            markedRows,
            selectedTodo,
            selectedProject.Title,
            selectedProject.Project?.Path ?? selectedProject.Error?.Path,
            selectedProject.Error?.Message,
            emptyMessage);
    }

    private static ImmutableArray<ProjectRow> BuildProjectRows(
        ProjectCatalog catalog,
        BrowserState state,
        DateOnly today)
    {
        var rows = new List<ProjectRow>
        {
            new(
                "All",
                catalog.Projects.Sum(project => CountActive(project.Todos)),
                null,
                null,
                state.ProjectIndex == 0,
                ProjectRowKind.All),
            new(
                "@today",
                catalog.Projects.Sum(project => CountActiveToday(project.Todos, today)),
                null,
                null,
                state.ProjectIndex == 1,
                ProjectRowKind.Today)
        };

        rows.AddRange(catalog.Projects.Select((project, index) => new ProjectRow(
            project.Title,
            CountActive(project.Todos),
            project,
            null,
            state.ProjectIndex == index + 2,
            ProjectRowKind.Project)));

        rows.AddRange(catalog.Errors.Select((error, index) => new ProjectRow(
            error.DisplayName,
            0,
            null,
            error,
            state.ProjectIndex == catalog.Projects.Length + index + 2,
            ProjectRowKind.Error)));

        return [.. rows];
    }

    private static ImmutableArray<TodoRow> BuildTodoRows(
        ProjectCatalog catalog,
        ProjectRow selectedProject,
        BrowserState state,
        DateOnly today)
    {
        if (selectedProject.Error is not null)
        {
            return [];
        }

        var projects = selectedProject.Project is null
            ? OrderProjects(catalog.Projects, state.Sort)
            : [selectedProject.Project];
        var rows = ImmutableArray.CreateBuilder<TodoRow>();
        var filter = EffectiveFilter(state);

        foreach (var project in projects)
        {
            var sourceTodos = Flatten(project.Todos, TodoSort.Source).ToArray();
            var sectionPaths = sourceTodos
                .Select(item => item.Todo.SectionPath)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var visibleForest = BuildVisibleForest(
                project.Todos,
                state.Sort,
                state.ShowCompleted,
                filter,
                selectedProject.Kind == ProjectRowKind.Today,
                today);
            var visibleTodos = FlattenVisible(visibleForest).ToArray();

            if ((filter.Length > 0 || selectedProject.Kind == ProjectRowKind.Today) &&
                visibleTodos.Length == 0)
            {
                continue;
            }

            if (selectedProject.Project is null)
            {
                rows.Add(new TodoRow(project.Title, null, [], false));
            }

            foreach (var sectionPath in sectionPaths)
            {
                var section = visibleTodos
                    .Where(item => item.Todo.SectionPath == sectionPath)
                    .ToArray();
                if (section.Length == 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(sectionPath))
                {
                    rows.Add(new TodoRow(sectionPath, null, [], false));
                }

                foreach (var item in section)
                {
                    rows.Add(new TodoRow(
                        null,
                        item.Todo,
                        item.TreePath,
                        false,
                        new TodoIdentity(project.Path, item.Todo.SourceLine))
                    {
                        ProjectTitle = project.Title
                    });
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
            || (todo.Schedule is not null &&
                (Contains(todo.Schedule.Date.ToString("yyyy-MM-dd"), filter) ||
                 Contains(todo.Schedule.Time?.ToString("HH:mm"), filter) ||
                 Contains(todo.Schedule.Time is null
                     ? todo.Schedule.Date.ToString("yyyy-MM-dd")
                     : $"{todo.Schedule.Date:yyyy-MM-dd} {todo.Schedule.Time:HH:mm}", filter)))
            || todo.Tags.Any(tag => Contains(tag, filter) || Contains($"#{tag}", filter));
    }

    private static bool Contains(string? value, string filter) =>
        value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;

    private static ImmutableArray<VisibleTodo> BuildVisibleForest(
        IEnumerable<TodoItem> todos,
        TodoSort sort,
        bool showCompleted,
        string filter,
        bool todayOnly,
        DateOnly today)
    {
        var visible = ImmutableArray.CreateBuilder<VisibleTodo>();

        foreach (var todo in OrderTodos(todos, sort))
        {
            var children = BuildVisibleForest(
                todo.Subtasks,
                sort,
                showCompleted,
                filter,
                todayOnly,
                today);
            if (!showCompleted && todo.IsCompleted)
            {
                visible.AddRange(children);
                continue;
            }

            var matchesToday = !todayOnly || todo.Schedule?.Date == today;
            var matchesFilter = filter.Length == 0 || MatchesFilter(todo, filter);
            if ((matchesToday && matchesFilter) || children.Length > 0)
            {
                visible.Add(new VisibleTodo(todo, children));
            }
        }

        return visible.ToImmutable();
    }

    private static IEnumerable<(TodoItem Todo, ImmutableArray<TodoTreeSegment> TreePath)> FlattenVisible(
        ImmutableArray<VisibleTodo> todos,
        ImmutableArray<TodoTreeSegment> parentPath = default)
    {
        for (var index = 0; index < todos.Length; index++)
        {
            var item = todos[index];
            yield return (item.Todo, parentPath.IsDefault ? [] : parentPath);

            var childParentPath = parentPath.IsDefault ? [] : parentPath;
            for (var childIndex = 0; childIndex < item.Children.Length; childIndex++)
            {
                var childPath = childParentPath.Add(
                    childIndex == item.Children.Length - 1
                        ? TodoTreeSegment.LastSibling
                        : TodoTreeSegment.HasFollowingSibling);
                foreach (var child in FlattenVisible([item.Children[childIndex]], childPath))
                {
                    yield return child;
                }
            }
        }
    }

    private static IEnumerable<(TodoItem Todo, int Depth)> Flatten(
        IEnumerable<TodoItem> todos,
        TodoSort sort,
        int depth = 0)
    {
        foreach (var todo in OrderTodos(todos, sort))
        {
            yield return (todo, depth);

            foreach (var subtask in Flatten(todo.Subtasks, sort, depth + 1))
            {
                yield return subtask;
            }
        }
    }

    private static IEnumerable<TodoProject> OrderProjects(
        IEnumerable<TodoProject> projects,
        TodoSort sort)
    {
        if (sort.Property != TodoSortProperty.File)
        {
            return projects;
        }

        var direction = sort.Direction == TodoSortDirection.Ascending ? 1 : -1;
        return projects.OrderBy(
            project => project,
            Comparer<TodoProject>.Create((left, right) =>
            {
                var filename = NaturalStringComparer.Instance.Compare(
                    Path.GetFileName(left.Path),
                    Path.GetFileName(right.Path));
                var result = filename != 0
                    ? filename
                    : StringComparer.OrdinalIgnoreCase.Compare(left.Path, right.Path);
                return result * direction;
            }));
    }

    private static IEnumerable<TodoItem> OrderTodos(IEnumerable<TodoItem> todos, TodoSort sort)
    {
        return todos.OrderBy(
            todo => todo,
            Comparer<TodoItem>.Create((left, right) => CompareTodos(left, right, sort)));
    }

    private static int CompareTodos(TodoItem left, TodoItem right, TodoSort sort)
    {
        var completion = left.IsCompleted.CompareTo(right.IsCompleted);
        if (completion != 0)
        {
            return completion;
        }

        var direction = sort.Direction == TodoSortDirection.Ascending ? 1 : -1;
        return sort.Property switch
        {
            TodoSortProperty.Name => NaturalStringComparer.Instance.Compare(left.Title, right.Title) * direction,
            TodoSortProperty.Schedule => CompareOptionalSchedules(left.Schedule, right.Schedule, direction),
            TodoSortProperty.Tags => CompareOptionalText(TagSortValue(left), TagSortValue(right), direction),
            TodoSortProperty.Priority => CompareOptionalPriorities(left.Priority, right.Priority, direction),
            _ => 0
        };
    }

    private static int CompareOptionalPriorities(
        TodoPriority? left,
        TodoPriority? right,
        int direction)
    {
        if (left is null || right is null)
        {
            return left is null == right is null ? 0 : left is null ? 1 : -1;
        }

        return left.Value.CompareTo(right.Value) * direction;
    }

    private static int CompareOptionalSchedules(TodoSchedule? left, TodoSchedule? right, int direction)
    {
        if (left is null || right is null)
        {
            return left is null == right is null ? 0 : left is null ? 1 : -1;
        }

        var date = left.Date.CompareTo(right.Date);
        var time = left.Time is null || right.Time is null
            ? left.Time is null == right.Time is null ? 0 : left.Time is null ? -1 : 1
            : left.Time.Value.CompareTo(right.Time.Value);
        return (date != 0 ? date : time) * direction;
    }

    private static int CompareOptionalText(string? left, string? right, int direction)
    {
        if (left is null || right is null)
        {
            return left is null == right is null ? 0 : left is null ? 1 : -1;
        }

        return NaturalStringComparer.Instance.Compare(left, right) * direction;
    }

    private static string? TagSortValue(TodoItem todo)
    {
        if (todo.Tags.Length == 0)
        {
            return null;
        }

        return string.Join(
            '\u001F',
            todo.Tags
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, NaturalStringComparer.Instance));
    }

    private static int CountActive(IEnumerable<TodoItem> todos) =>
        Flatten(todos, TodoSort.Source).Count(item => !item.Todo.IsCompleted);

    private static int CountActiveToday(IEnumerable<TodoItem> todos, DateOnly today) =>
        Flatten(todos, TodoSort.Source).Count(item =>
            !item.Todo.IsCompleted && item.Todo.Schedule?.Date == today);

    private static bool HasCompletedTodos(
        ProjectRow selectedProject,
        ProjectCatalog catalog,
        DateOnly today)
    {
        var projects = selectedProject.Project is null
            ? catalog.Projects
            : [selectedProject.Project];

        return projects
            .SelectMany(project => Flatten(project.Todos, TodoSort.Source))
            .Any(item => item.Todo.IsCompleted &&
                         (selectedProject.Kind != ProjectRowKind.Today ||
                          item.Todo.Schedule?.Date == today));
    }

    private sealed record VisibleTodo(TodoItem Todo, ImmutableArray<VisibleTodo> Children);
}
