using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls.Rendering;
using Wolf.Controls.SelectList;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.Commands.Rendering;

/// <summary>Places the command palette within a rendered workspace.</summary>
internal sealed class FloatingCommandPalette(
    IRenderable workspace,
    SelectListState palette,
    TuiTheme theme,
    int terminalWidth,
    int terminalHeight,
    string selectionMarker) : IRenderable
{
    public static bool CanFloat(int width, int height) => width >= 80 && height >= 24;

    public Measurement Measure(RenderOptions options, int maxWidth) => workspace.Measure(options, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Clamp(terminalWidth, 1, Math.Max(1, maxWidth));
        var lines = Segment.SplitLines(workspace.Render(options, width), width, null);
        var height = Math.Max(1, lines.Count);
        var panelWidth = Math.Min(88, width - 8);
        var halfHeight = Math.Max(1, (height - 2) / 2);
        var extraRows = (palette.SearchText is null ? 0 : 1) + (palette.Error is null ? 0 : 1);
        var optionRows = Math.Max(1, Math.Min(
            Math.Clamp(terminalHeight / 5, 3, 7), halfHeight - 3 - extraRows));
        var constraints = new ControlConstraints(panelWidth, optionRows);
        var displayPalette = FitPalette(palette, panelWidth);
        var panelHeight = SelectList.Default.Measure(displayPalette, constraints);
        var selectedRow = lines.FindIndex(line =>
            string.Concat(line.Select(segment => segment.Text)).Contains(selectionMarker, StringComparison.Ordinal));
        var alignment = selectedRow >= 0 && selectedRow < height / 2
            ? OverlayAlignment.Bottom
            : OverlayAlignment.Top;
        var panel = new SurfaceThemeRenderer().OnSurface(
            TuiControlAdapter.Render(displayPalette, theme, constraints),
            theme.Surface2);
        var overlay = OverlayLayout.Place(panel, width, height, alignment,
            width: panelWidth, height: panelHeight, top: 1, bottom: 1, zIndex: 1);
        return new LayeredRenderable(width, height,
            [new ControlLayer(workspace, 0, 0), overlay]).Render(options, width)
            .Concat([Segment.LineBreak]);
    }

    private static SelectListState FitPalette(SelectListState state, int panelWidth)
    {
        var contentWidth = Math.Max(1, panelWidth - 4);
        var options = state.Options.Select(option =>
        {
            var label = Fit(option.Label, Math.Max(1, contentWidth * 2 / 3));
            var detail = option.Detail is null
                ? null
                : Fit(option.Detail, Math.Max(1, contentWidth - 4 - label.GetCellWidth()));
            return option with { Label = label, Detail = detail };
        }).ToArray();
        return state with
        {
            Options = options,
            SearchText = state.SearchText is null ? null : Fit(state.SearchText, contentWidth - 2),
            EmptyMessage = Fit(state.EmptyMessage, contentWidth),
            Footer = Fit(state.Footer, contentWidth),
            Error = state.Error is null ? null : Fit(state.Error, contentWidth)
        };
    }

    private static string Fit(string value, int width) => TerminalText.Ellipsize(value, width);
}
