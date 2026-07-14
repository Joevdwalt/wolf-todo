using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Tabs;

public sealed record TabStripView(ImmutableArray<TabItemView> Tabs);
