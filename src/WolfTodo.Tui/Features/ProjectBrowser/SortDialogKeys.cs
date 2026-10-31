namespace WolfTodo.Tui.Features.ProjectBrowser;

public static class SortDialogKeys
{
    public static TodoSort? Resolve(ConsoleKeyInfo key) => key.KeyChar switch
    {
        'n' => new TodoSort(TodoSortProperty.Name, TodoSortDirection.Ascending),
        'N' => new TodoSort(TodoSortProperty.Name, TodoSortDirection.Descending),
        'd' => new TodoSort(TodoSortProperty.Schedule, TodoSortDirection.Ascending),
        'D' => new TodoSort(TodoSortProperty.Schedule, TodoSortDirection.Descending),
        't' => new TodoSort(TodoSortProperty.Tags, TodoSortDirection.Ascending),
        'T' => new TodoSort(TodoSortProperty.Tags, TodoSortDirection.Descending),
        'f' => new TodoSort(TodoSortProperty.File, TodoSortDirection.Ascending),
        'F' => new TodoSort(TodoSortProperty.File, TodoSortDirection.Descending),
        'p' => new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Ascending),
        'P' => new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Descending),
        'o' => TodoSort.Source,
        _ => null
    };
}
