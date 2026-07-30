using System.Collections.Immutable;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public static class ApplicationTabs
{
    public static TabId Todos { get; } = new("todos");

    public static TabId Planner { get; } = new("planner");

    public static ImmutableArray<TabDefinition> All { get; } =
    [
        new(Todos, "Todos"),
        new(Planner, "Day Planner")
    ];
}
