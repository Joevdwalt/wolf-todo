namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record BrowserState(
    BrowserFocus Focus,
    int ProjectIndex,
    int TodoIndex,
    bool ShowCompleted,
    bool IsCommandMode,
    string Command,
    bool IsFilterMode,
    string FilterText,
    string FilterDraft,
    string? Error)
{
    public static BrowserState Initial { get; } = new(
        BrowserFocus.Projects,
        0,
        0,
        false,
        false,
        string.Empty,
        false,
        string.Empty,
        string.Empty,
        null);
}
