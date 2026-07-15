using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record CommandPaletteView(
    CommandPaletteState State,
    ImmutableArray<CommandPaletteItem> Items)
{
    public int SelectedIndex => Items.Length == 0
        ? 0
        : Math.Clamp(State.SelectedIndex, 0, Items.Length - 1);

    public CommandPaletteItem? SelectedItem => Items.Length == 0 ? null : Items[SelectedIndex];
}
