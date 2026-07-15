using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class CommandPalettePresenter
{
    public CommandPaletteView CreateView(
        CommandPaletteState state,
        ImmutableArray<CommandPaletteItem> items)
    {
        var query = state.Query.Trim();
        var filtered = query.Length == 0
            ? items
            : [.. items.Where(item => Matches(item, query))];
        return new CommandPaletteView(state, filtered);
    }

    private static bool Matches(CommandPaletteItem item, string query) =>
        item.Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        item.Binding.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        item.Group.Contains(query, StringComparison.OrdinalIgnoreCase);
}
