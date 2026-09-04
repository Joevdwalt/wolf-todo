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

        if (IsSelectAll(key))
        {
            var selected = state.Text.Length == 0
                ? state with { SelectionAnchor = null }
                : state with { Cursor = state.Text.Length, SelectionAnchor = 0 };
            return new(selected, MultilineTextBoxOutcome.Editing);
        }

        return new(ReduceEditing(state, key), MultilineTextBoxOutcome.Editing);
    }

    private static MultilineTextBoxState ReduceEditing(MultilineTextBoxState state, ConsoleKeyInfo key)
    {
        var cursor = state.ClampedCursor;
        if (state.HasSelection)
        {
            if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.UpArrow or ConsoleKey.Home)
            {
                return state with { Cursor = state.SelectionStart, SelectionAnchor = null };
            }

            if (key.Key is ConsoleKey.RightArrow or ConsoleKey.DownArrow or ConsoleKey.End)
            {
                return state with
                {
                    Cursor = state.SelectionStart + state.SelectionLength,
                    SelectionAnchor = null
                };
            }

            if (key.Key is ConsoleKey.Backspace or ConsoleKey.Delete)
            {
                return ReplaceSelection(state, string.Empty);
            }

            if (key.Key == ConsoleKey.Enter && state.IsMultiline)
            {
                return ReplaceSelection(state, "\n");
            }

            if (!char.IsControl(key.KeyChar))
            {
                return ReplaceSelection(state, key.KeyChar.ToString());
            }
        }

        return key.Key switch
        {
            ConsoleKey.LeftArrow => state with
            {
                Cursor = Math.Max(0, cursor - 1),
                SelectionAnchor = null
            },
            ConsoleKey.RightArrow => state with
            {
                Cursor = Math.Min(state.Text.Length, cursor + 1),
                SelectionAnchor = null
            },
            ConsoleKey.Home => state with
            {
                Cursor = LineStart(state.Text, cursor),
                SelectionAnchor = null
            },
            ConsoleKey.End => state with
            {
                Cursor = LineEnd(state.Text, cursor),
                SelectionAnchor = null
            },
            ConsoleKey.UpArrow => state with
            {
                Cursor = MoveLine(state.Text, cursor, -1),
                SelectionAnchor = null
            },
            ConsoleKey.DownArrow => state with
            {
                Cursor = MoveLine(state.Text, cursor, 1),
                SelectionAnchor = null
            },
            ConsoleKey.Backspace when cursor > 0 => state with
            {
                Text = state.Text.Remove(cursor - 1, 1),
                Cursor = cursor - 1,
                SelectionAnchor = null
            },
            ConsoleKey.Delete when cursor < state.Text.Length => state with
            {
                Text = state.Text.Remove(cursor, 1),
                SelectionAnchor = null
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
            Cursor = cursor + value.Length,
            SelectionAnchor = null
        };
    }

    private static MultilineTextBoxState ReplaceSelection(MultilineTextBoxState state, string value)
    {
        var text = state.Text.Remove(state.SelectionStart, state.SelectionLength)
            .Insert(state.SelectionStart, value);
        return state with
        {
            Text = text,
            Cursor = state.SelectionStart + value.Length,
            SelectionAnchor = null
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
        var lineStart = lines.Take(start).Sum(line => line.Length + 1);
        for (var index = start; index < Math.Min(lines.Length, start + visibleRows); index++)
        {
            var line = lines[index];
            if (state.HasSelection)
            {
                renderLines.Add(CreateSelectedLine(line, lineStart, state, theme));
                lineStart += line.Length + 1;
                continue;
            }

            if (index != cursorLine)
            {
                renderLines.Add(new Text(line, new Style(theme.Text)));
                lineStart += line.Length + 1;
                continue;
            }

            var cursorLineStart = state.ClampedCursor == 0
                ? 0
                : state.Text.LastIndexOf('\n', state.ClampedCursor - 1) + 1;
            var column = Math.Max(0, state.ClampedCursor - cursorLineStart);
            var before = line[..Math.Min(column, line.Length)];
            var after = line[Math.Min(column, line.Length)..];
            renderLines.Add(new Text(before + "▏" + after,
                new Style(theme.AccentBright, decoration: Decoration.Bold)));
            lineStart += line.Length + 1;
        }

        while (renderLines.Count < visibleRows)
        {
            renderLines.Add(new Text(string.Empty));
        }

        renderLines.Add(new Text($"{saveBinding} SAVE TEXT  Esc CANCEL", new Style(theme.Muted, decoration: Decoration.Dim)));
        return TuiControlPanel.Create(state.Label, new Rows(renderLines), theme);
    }

    private static bool IsSelectAll(ConsoleKeyInfo key) =>
        key.Key == ConsoleKey.A && key.Modifiers.HasFlag(ConsoleModifiers.Control);

    private static IRenderable CreateSelectedLine(
        string line,
        int lineStart,
        MultilineTextBoxState state,
        TuiTheme theme)
    {
        var selectionStart = Math.Clamp(state.SelectionStart - lineStart, 0, line.Length);
        var selectionEnd = Math.Clamp(
            state.SelectionStart + state.SelectionLength - lineStart,
            selectionStart,
            line.Length);
        var textStyle = new Style(theme.Text);
        var selectionStyle = new Style(theme.Background, theme.AccentBright, Decoration.Bold);
        return new Columns(
        [
            new Text(line[..selectionStart], textStyle),
            new Text(line[selectionStart..selectionEnd], selectionStyle),
            new Text(line[selectionEnd..], textStyle)
        ])
        {
            Padding = new Padding(0),
            Expand = false
        };
    }
}
