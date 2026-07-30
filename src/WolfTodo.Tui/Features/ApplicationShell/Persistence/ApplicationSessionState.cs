using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell.Persistence;

public sealed record ApplicationSessionState(string? SelectedProjectPath, TodoSort Sort)
{
    public static ApplicationSessionState Initial { get; } = new(null, TodoSort.Source);
}
