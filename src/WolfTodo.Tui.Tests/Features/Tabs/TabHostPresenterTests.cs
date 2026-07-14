using FluentAssertions;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Tests.Features.Tabs;

public sealed class TabHostPresenterTests
{
    private readonly TabHostPresenter presenter = new();

    [Fact]
    public void CreateView_preserves_order_and_marks_the_active_tab()
    {
        var todos = new TabId("todos");
        var planner = new TabId("planner");

        var result = presenter.CreateView(
            [new TabDefinition(todos, "Todos"), new TabDefinition(planner, "Day Planner")],
            new TabHostState(planner));

        result.Tabs.Select(tab => tab.Title).Should().Equal("Todos", "Day Planner");
        result.Tabs.Select(tab => tab.IsSelected).Should().Equal(false, true);
    }

    [Fact]
    public void CreateView_rejects_an_unregistered_active_tab()
    {
        var action = () => presenter.CreateView(
            [new TabDefinition(new TabId("todos"), "Todos")],
            new TabHostState(new TabId("missing")));

        action.Should().Throw<InvalidOperationException>();
    }
}
