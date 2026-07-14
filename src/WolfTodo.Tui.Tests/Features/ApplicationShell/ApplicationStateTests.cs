using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class ApplicationStateTests
{
    [Fact]
    public void With_a_different_active_tab_preserves_browser_state()
    {
        var browser = BrowserState.Initial with { FilterText = "renewal", TodoIndex = 3 };
        var state = new ApplicationState(new TabHostState(new TabId("todos")), browser);

        var switched = state with { Tabs = new TabHostState(new TabId("planner")) };
        var returned = switched with { Tabs = new TabHostState(new TabId("todos")) };

        returned.Browser.Should().Be(browser);
    }
}
