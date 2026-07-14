using FluentAssertions;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Tests.Features.Tabs;

public sealed class TabHostReducerTests
{
    private readonly TabHostReducer reducer = new();
    private static readonly TabId Todos = new("todos");
    private static readonly TabId Planner = new("planner");
    private static readonly TabDefinition[] TabArray =
    [
        new(Todos, "Todos"),
        new(Planner, "Day Planner")
    ];

    [Fact]
    public void Move_wraps_next_from_the_last_tab()
    {
        var result = reducer.Move(new TabHostState(Planner), [.. TabArray], TabDirection.Next);

        result.ActiveTab.Should().Be(Todos);
    }

    [Fact]
    public void Move_wraps_previous_from_the_first_tab()
    {
        var result = reducer.Move(new TabHostState(Todos), [.. TabArray], TabDirection.Previous);

        result.ActiveTab.Should().Be(Planner);
    }

    [Fact]
    public void Move_is_a_no_op_with_one_tab()
    {
        var result = reducer.Move(
            new TabHostState(Todos),
            [new TabDefinition(Todos, "Todos")],
            TabDirection.Next);

        result.ActiveTab.Should().Be(Todos);
    }

    [Fact]
    public void Move_rejects_an_unregistered_active_tab()
    {
        var action = () => reducer.Move(
            new TabHostState(new TabId("missing")),
            [.. TabArray],
            TabDirection.Next);

        action.Should().Throw<InvalidOperationException>();
    }
}
