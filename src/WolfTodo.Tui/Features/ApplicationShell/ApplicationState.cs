using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationState(TabHostState Tabs, BrowserState Browser)
{
    public static ApplicationState CreateInitial(TabHostState tabs) => new(tabs, BrowserState.Initial);
}
