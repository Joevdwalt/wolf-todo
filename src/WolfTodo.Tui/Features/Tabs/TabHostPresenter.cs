using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Tabs;

public sealed class TabHostPresenter
{
    public TabStripView CreateView(ImmutableArray<TabDefinition> tabs, TabHostState state)
    {
        if (tabs.Length == 0)
        {
            throw new ArgumentException("At least one tab is required.", nameof(tabs));
        }

        if (!tabs.Any(tab => tab.Id == state.ActiveTab))
        {
            throw new InvalidOperationException($"Active tab '{state.ActiveTab.Value}' is not registered.");
        }

        return new TabStripView(
            [.. tabs.Select(tab => new TabItemView(tab.Id, tab.Title, tab.Id == state.ActiveTab))]);
    }
}
