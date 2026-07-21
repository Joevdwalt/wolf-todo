using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

internal static class TextBoxControl
{
    public static int Height(TextBoxState state, int maxRows) => Math.Max(1, maxRows) + 3;

    public static IRenderable Create(string title, TextBoxState state, TuiTheme theme, int maxRows, string saveBinding)
    {
        var lines = state.Text.Split('\n');
        var cursorLine = state.Text[..state.ClampedCursor].Count(character => character == '\n');
        var visibleRows = Math.Max(1, maxRows);
        var start = Math.Clamp(cursorLine - visibleRows + 1, 0, Math.Max(0, lines.Length - visibleRows));
        var renderLines = new List<IRenderable>();
        for (var index = start; index < Math.Min(lines.Length, start + visibleRows); index++)
        {
            var line = lines[index];
            if (index != cursorLine)
            {
                renderLines.Add(new Text(line, new Style(theme.Text)));
                continue;
            }

            var lineStart = state.Text.LastIndexOf('\n', Math.Max(0, state.ClampedCursor - 1)) + 1;
            var column = state.ClampedCursor - lineStart;
            var before = line[..Math.Min(column, line.Length)];
            var after = line[Math.Min(column, line.Length)..];
            renderLines.Add(new Text(before + "▏" + after,
                new Style(theme.AccentBright, decoration: Decoration.Bold)));
        }

        while (renderLines.Count < visibleRows)
        {
            renderLines.Add(new Text(string.Empty));
        }

        renderLines.Add(new Text($"{saveBinding} SAVE TEXT  Esc CANCEL", new Style(theme.Muted, decoration: Decoration.Dim)));
        return TuiControlPanel.Create(title, new Rows(renderLines), theme);
    }
}
