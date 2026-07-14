using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Tabs;

public sealed class TabHostReducer
{
    public TabHostState Move(
        TabHostState state,
        ImmutableArray<TabDefinition> tabs,
        TabDirection direction)
    {
        if (tabs.Length == 0)
        {
            throw new ArgumentException("At least one tab is required.", nameof(tabs));
        }

        var currentIndex = -1;

        for (var index = 0; index < tabs.Length; index++)
        {
            if (tabs[index].Id == state.ActiveTab)
            {
                currentIndex = index;
                break;
            }
        }

        if (currentIndex < 0)
        {
            throw new InvalidOperationException($"Active tab '{state.ActiveTab.Value}' is not registered.");
        }

        var offset = direction == TabDirection.Next ? 1 : -1;
        var nextIndex = (currentIndex + offset + tabs.Length) % tabs.Length;
        return state with { ActiveTab = tabs[nextIndex].Id };
    }
}
