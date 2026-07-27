using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

public sealed class SelectList : ITuiComponent<SelectListView, SelectListOutcome>
{
    public static SelectList Default { get; } = new();

    public TuiComponentTransition<SelectListView, SelectListOutcome> Reduce(
        SelectListView state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (bindings.MatchesBack(key))
        {
            return new(null, SelectListOutcome.Cancelled);
        }

        if (bindings.MatchesOpen(key) && state.Options.Count > 0)
        {
            return new(state, SelectListOutcome.Accepted);
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            var selectedIndex = Math.Clamp(state.SelectedIndex + offset, 0, Math.Max(0, state.Options.Count - 1));
            var updated = state with { SelectedIndex = selectedIndex };
            return new(updated, selectedIndex == state.SelectedIndex
                ? SelectListOutcome.Editing
                : SelectListOutcome.SelectionChanged);
        }

        return new(state, SelectListOutcome.Editing);
    }

    public int Measure(SelectListView view, TuiComponentConstraints constraints)
    {
        var optionRows = Math.Max(1, Math.Min(constraints.ClampedMaxRows, view.Options.Count));
        return 2 + optionRows + 1 + (view.SearchText is null ? 0 : 1) + (view.Error is null ? 0 : 1);
    }

    public IRenderable Render(SelectListView view, TuiTheme theme, TuiComponentConstraints constraints)
    {
        var lines = new List<IRenderable>();
        if (view.SearchText is not null)
        {
            lines.Add(new Text($"/{view.SearchText}_", new Style(theme.Accent, decoration: Decoration.Bold)));
        }

        if (view.Options.Count == 0)
        {
            lines.Add(new Text(view.EmptyMessage, new Style(theme.Muted, decoration: Decoration.Dim)));
        }
        else
        {
            var visibleRows = Math.Max(1, Math.Min(constraints.ClampedMaxRows, view.Options.Count));
            var start = Math.Clamp(
                view.ClampedSelectedIndex - visibleRows + 1,
                0,
                Math.Max(0, view.Options.Count - visibleRows));
            for (var index = start; index < start + visibleRows; index++)
            {
                var option = view.Options[index];
                var selected = index == view.ClampedSelectedIndex;
                var color = !option.IsEnabled ? theme.Muted : selected ? theme.AccentBright : theme.Text;
                var decoration = !option.IsEnabled ? Decoration.Dim : selected ? Decoration.Bold : Decoration.None;
                var detail = string.IsNullOrWhiteSpace(option.Detail) ? string.Empty : $"  {option.Detail}";
                lines.Add(new Text($"{(selected ? ">" : " ")} {option.Label}{detail}",
                    new Style(color, decoration: decoration)).Ellipsis());
            }
        }

        if (view.Error is not null)
        {
            lines.Add(new Text(view.Error, new Style(theme.Error, decoration: Decoration.Bold)).Ellipsis());
        }

        lines.Add(new Text(view.Footer, new Style(theme.Muted, decoration: Decoration.Dim)).Ellipsis());
        return TuiControlPanel.Create(view.Title, new Rows(lines), theme);
    }
}

public enum SelectListOutcome
{
    Editing,
    SelectionChanged,
    Accepted,
    Cancelled
}
