using FluentAssertions;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Tests.Features.Tabs;

public sealed class TabHostStateTests
{
    [Fact]
    public void CreateInitial_selects_the_first_tab()
    {
        var result = TabHostState.CreateInitial(
            [new TabDefinition(new TabId("todos"), "Todos"), new TabDefinition(new TabId("planner"), "Planner")]);

        result.ActiveTab.Should().Be(new TabId("todos"));
    }

    [Fact]
    public void CreateInitial_rejects_an_empty_tab_list()
    {
        var action = () => TabHostState.CreateInitial([]);

        action.Should().Throw<ArgumentException>();
    }
}
