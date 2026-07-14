using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Tabs;

public sealed record TabHostState(TabId ActiveTab)
{
    public static TabHostState CreateInitial(ImmutableArray<TabDefinition> tabs)
    {
        if (tabs.Length == 0)
        {
            throw new ArgumentException("At least one tab is required.", nameof(tabs));
        }

        return new TabHostState(tabs[0].Id);
    }
}
