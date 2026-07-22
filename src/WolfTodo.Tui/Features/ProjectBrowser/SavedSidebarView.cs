namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record SavedSidebarView(string Title, SavedTodoQuery Query, TodoSort Order);
