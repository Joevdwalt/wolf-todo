namespace WolfTodo.Tui.Features.ProjectBrowser;

public static class TodoSelectionResolver
{
    public static int ResolveIndex(TodoRow[] rows, BrowserState state, Dictionary<TodoIdentity, TodoIdentity> ancestors)
    {
        var pending = state.PendingTodoSelection;
        var index = pending is null ? -1 : Array.FindIndex(rows, row => row.Identity == pending);
        while (index < 0 && pending is not null && ancestors.TryGetValue(pending, out var ancestor))
        {
            pending = ancestor;
            index = Array.FindIndex(rows, row => row.Identity == pending);
        }
        return state.PendingTodoSelection is null
            ? Math.Clamp(state.TodoIndex, 0, Math.Max(0, rows.Length - 1))
            : Math.Max(0, index);
    }

    public static TodoIdentity? AfterCompletion(BrowserView before, BrowserView after, TodoIdentity identity)
    {
        var remaining = after.Todos.Where(row => row.Todo is not null && row.Identity is not null)
            .Select(row => row.Identity!).ToArray();
        if (before.State.ShowCompleted && remaining.Contains(identity)) return identity;

        var previous = before.Todos.Where(row => row.Todo is not null && row.Identity is not null)
            .Select(row => row.Identity!).ToArray();
        var index = Array.IndexOf(previous, identity);
        if (index >= 0)
        {
            for (var i = index - 1; i >= 0; i--)
                if (remaining.Contains(previous[i])) return previous[i];
            for (var i = index + 1; i < previous.Length; i++)
                if (remaining.Contains(previous[i])) return previous[i];
        }
        return remaining.FirstOrDefault();
    }
}
