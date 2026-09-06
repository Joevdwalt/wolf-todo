using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls;

/// <summary>A themed, scrollable terminal selection control.</summary>
public sealed class SelectList : IControl<SelectListState, SelectListOutcome>
{
    public static SelectList Default { get; } = new();

    public ControlTransition<SelectListState, SelectListOutcome> Reduce(SelectListState state, ControlInput input)
    {
        if (input.Kind == ControlInputKind.Cancel)
        {
            return new(null, SelectListOutcome.Cancelled);
        }

        if (input.Kind == ControlInputKind.Accept && state.SelectedOption?.IsEnabled == true)
        {
            return new(state, SelectListOutcome.Accepted);
        }

        if (input.Kind is not (ControlInputKind.MoveUp or ControlInputKind.MoveDown))
        {
            return new(state, SelectListOutcome.Editing);
        }

        var next = FindNextEnabled(state.Options, state.ClampedSelectedIndex, input.Kind == ControlInputKind.MoveUp ? -1 : 1);
        if (next == state.ClampedSelectedIndex)
        {
            return new(state, SelectListOutcome.Editing);
        }

        return new(state with { SelectedIndex = next }, SelectListOutcome.SelectionChanged);
    }

    public int Measure(SelectListState state, ControlConstraints constraints)
    {
        var optionRows = Math.Max(1, Math.Min(constraints.ClampedMaxRows, state.Options.Count));
        return 2 + optionRows + 1 + (state.SearchText is null ? 0 : 1) + (state.Error is null ? 0 : 1);
    }

    public IRenderable Render(SelectListState state, ControlTheme theme, ControlConstraints constraints)
    {
        var lines = new List<IRenderable>();
        if (state.SearchText is not null)
        {
            lines.Add(new Text($"/{state.SearchText}_", new Style(theme.Accent, decoration: Decoration.Bold)));
        }

        if (state.Options.Count == 0)
        {
            lines.Add(new Text(state.EmptyMessage, new Style(theme.Muted, decoration: Decoration.Dim)));
        }
        else
        {
            var visibleRows = Math.Max(1, Math.Min(constraints.ClampedMaxRows, state.Options.Count));
            var selected = state.ClampedSelectedIndex;
            var start = Math.Clamp(selected - visibleRows + 1, 0, Math.Max(0, state.Options.Count - visibleRows));
            for (var index = start; index < start + visibleRows; index++)
            {
                var option = state.Options[index];
                var isSelected = index == selected;
                var color = !option.IsEnabled ? theme.Muted : isSelected ? theme.AccentBright : theme.Text;
                var decoration = !option.IsEnabled ? Decoration.Dim : isSelected ? Decoration.Bold : Decoration.None;
                var detail = string.IsNullOrWhiteSpace(option.Detail) ? string.Empty : $"  {option.Detail}";
                lines.Add(new Text($"{(isSelected ? ">" : " ")} {option.Label}{detail}", new Style(color, decoration: decoration)).Ellipsis());
            }
        }

        if (state.Error is not null)
        {
            lines.Add(new Text(state.Error, new Style(theme.Error, decoration: Decoration.Bold)).Ellipsis());
        }

        lines.Add(new Text(state.Footer, new Style(theme.Muted, decoration: Decoration.Dim)).Ellipsis());
        return new Panel(new Rows(lines))
        {
            Header = new PanelHeader(state.Title.ToUpperInvariant()),
            Border = BoxBorder.Square,
            BorderStyle = new Style(theme.Border),
            Padding = new Padding(1, 0),
            Width = constraints.ClampedWidth,
            Expand = false
        };
    }

    private static int FindNextEnabled(IReadOnlyList<SelectOption> options, int selected, int direction)
    {
        if (options.Count == 0)
        {
            return -1;
        }

        var start = selected < 0 ? (direction > 0 ? -1 : options.Count) : selected;
        for (var candidate = start + direction; candidate >= 0 && candidate < options.Count; candidate += direction)
        {
            if (options[candidate].IsEnabled)
            {
                return candidate;
            }
        }

        return selected;
    }
}
