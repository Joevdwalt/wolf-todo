using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

public sealed class MultilineTextBox : ITuiComponent<MultilineTextBoxState, MultilineTextBoxOutcome>
{
    public static MultilineTextBox Default { get; } = new();

    public TuiComponentTransition<MultilineTextBoxState, MultilineTextBoxOutcome> Reduce(
        MultilineTextBoxState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return new(null, MultilineTextBoxOutcome.Cancelled);
        }

        if (bindings.MatchesSaveForm(key))
        {
            return new(state, MultilineTextBoxOutcome.Accepted);
        }

        return new(ReduceEditing(state, key), MultilineTextBoxOutcome.Editing);
    }

    private static MultilineTextBoxState ReduceEditing(MultilineTextBoxState state, ConsoleKeyInfo key)
    {
        var cursor = state.ClampedCursor;
        return key.Key switch
        {
            ConsoleKey.LeftArrow => state with { Cursor = Math.Max(0, cursor - 1) },
            ConsoleKey.RightArrow => state with { Cursor = Math.Min(state.Text.Length, cursor + 1) },
            ConsoleKey.Home => state with { Cursor = LineStart(state.Text, cursor) },
            ConsoleKey.End => state with { Cursor = LineEnd(state.Text, cursor) },
            ConsoleKey.UpArrow => state with { Cursor = MoveLine(state.Text, cursor, -1) },
            ConsoleKey.DownArrow => state with { Cursor = MoveLine(state.Text, cursor, 1) },
            ConsoleKey.Backspace when cursor > 0 => state with
            {
                Text = state.Text.Remove(cursor - 1, 1),
                Cursor = cursor - 1
            },
            ConsoleKey.Delete when cursor < state.Text.Length => state with
            {
                Text = state.Text.Remove(cursor, 1)
            },
            ConsoleKey.Enter when state.IsMultiline => Insert(state, "\n"),
            _ when !char.IsControl(key.KeyChar) => Insert(state, key.KeyChar.ToString()),
            _ => state
        };
    }

    private static MultilineTextBoxState Insert(MultilineTextBoxState state, string value)
    {
        var cursor = state.ClampedCursor;
        return state with
        {
            Text = state.Text.Insert(cursor, value),
            Cursor = cursor + value.Length
        };
    }

    private static int LineStart(string text, int cursor) => text.LastIndexOf('\n', Math.Max(0, cursor - 1)) + 1;

    private static int LineEnd(string text, int cursor)
    {
        var end = text.IndexOf('\n', cursor);
        return end < 0 ? text.Length : end;
    }

    private static int MoveLine(string text, int cursor, int offset)
    {
        var start = LineStart(text, cursor);
        var column = cursor - start;
        if (offset < 0)
        {
            if (start == 0)
            {
                return cursor;
            }

            var previousEnd = start - 1;
            var previousStart = LineStart(text, previousEnd);
            return Math.Min(previousStart + column, previousEnd);
        }

        var end = LineEnd(text, cursor);
        if (end == text.Length)
        {
            return cursor;
        }

        var nextStart = end + 1;
        return Math.Min(nextStart + column, LineEnd(text, nextStart));
    }

    public int Measure(MultilineTextBoxState state, TuiComponentConstraints constraints) =>
        constraints.ClampedMaxRows + 3;

    public IRenderable Render(MultilineTextBoxState state, TuiTheme theme, TuiComponentConstraints constraints) =>
        Render(state, theme, constraints, "Ctrl+S");

    public IRenderable Render(
        MultilineTextBoxState state,
        TuiTheme theme,
        TuiComponentConstraints constraints,
        string saveBinding)
    {
        var lines = state.Text.Split('\n');
        var cursorLine = state.Text[..state.ClampedCursor].Count(character => character == '\n');
        var visibleRows = constraints.ClampedMaxRows;
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
        return TuiControlPanel.Create(state.Label, new Rows(renderLines), theme);
    }
}
