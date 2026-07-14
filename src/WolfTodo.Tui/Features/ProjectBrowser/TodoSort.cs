namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoSort(TodoSortProperty Property, TodoSortDirection Direction)
{
    public static TodoSort Source { get; } = new(TodoSortProperty.Source, TodoSortDirection.Ascending);
}
