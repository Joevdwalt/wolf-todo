using Spectre.Console;
using Spectre.Console.Rendering;
namespace Wolf.Controls;

public sealed class MultilineTextBox : IControl<MultilineTextBoxState, MultilineTextBoxOutcome>
{
    public static MultilineTextBox Default { get; } = new();

    public ControlTransition<MultilineTextBoxState, MultilineTextBoxOutcome> Reduce(
        MultilineTextBoxState state,
        ControlInput input)
    {
        if (input.Kind == ControlInputKind.Cancel)
        {
            return new(null, MultilineTextBoxOutcome.Cancelled);
        }

        if (input.Kind == ControlInputKind.Accept)
        {
            return new(state, MultilineTextBoxOutcome.Accepted);
        }

        if (input.Kind == ControlInputKind.SelectAll)
        {
            var selected = state.Text.Length == 0
                ? state with { SelectionAnchor = null }
                : state with { Cursor = state.Text.Length, SelectionAnchor = 0 };
            return new(selected, MultilineTextBoxOutcome.Editing);
        }

        return new(ReduceEditing(state, input), MultilineTextBoxOutcome.Editing);
    }

    private static MultilineTextBoxState ReduceEditing(MultilineTextBoxState state, ControlInput input)
    {
        var cursor = state.ClampedCursor;
        if (state.HasSelection)
        {
            if (input.Kind is ControlInputKind.MoveLeft or ControlInputKind.MoveUp or ControlInputKind.MoveHome)
            {
                return state with { Cursor = state.SelectionStart, SelectionAnchor = null };
            }

            if (input.Kind is ControlInputKind.MoveRight or ControlInputKind.MoveDown or ControlInputKind.MoveEnd)
            {
                return state with
                {
                    Cursor = state.SelectionStart + state.SelectionLength,
                    SelectionAnchor = null
                };
            }

            if (input.Kind is ControlInputKind.Backspace or ControlInputKind.Delete)
            {
                return ReplaceSelection(state, string.Empty);
            }

            if (input.Kind == ControlInputKind.InsertNewline && state.IsMultiline)
            {
                return ReplaceSelection(state, "\n");
            }

            if (input.Kind == ControlInputKind.Character && !char.IsControl(input.Character))
            {
                return ReplaceSelection(state, input.Character.ToString());
            }
        }

        return input.Kind switch
        {
            ControlInputKind.MoveLeft => state with
            {
                Cursor = Math.Max(0, cursor - 1),
                SelectionAnchor = null
            },
            ControlInputKind.MoveRight => state with
            {
                Cursor = Math.Min(state.Text.Length, cursor + 1),
                SelectionAnchor = null
            },
            ControlInputKind.MoveHome => state with
            {
                Cursor = LineStart(state.Text, cursor),
                SelectionAnchor = null
            },
            ControlInputKind.MoveEnd => state with
            {
                Cursor = LineEnd(state.Text, cursor),
                SelectionAnchor = null
            },
            ControlInputKind.MoveUp => state with
            {
                Cursor = MoveLine(state.Text, cursor, -1),
                SelectionAnchor = null
            },
            ControlInputKind.MoveDown => state with
            {
                Cursor = MoveLine(state.Text, cursor, 1),
                SelectionAnchor = null
            },
            ControlInputKind.Backspace when cursor > 0 => state with
            {
                Text = state.Text.Remove(cursor - 1, 1),
                Cursor = cursor - 1,
                SelectionAnchor = null
            },
            ControlInputKind.Delete when cursor < state.Text.Length => state with
            {
                Text = state.Text.Remove(cursor, 1),
                SelectionAnchor = null
            },
            ControlInputKind.InsertNewline when state.IsMultiline => Insert(state, "\n"),
            ControlInputKind.Character when !char.IsControl(input.Character) => Insert(state, input.Character.ToString()),
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

    public int Measure(MultilineTextBoxState state, ControlConstraints constraints) =>
        constraints.ClampedMaxRows + 3;

    public IRenderable Render(
        MultilineTextBoxState state,
        ControlTheme theme,
        ControlConstraints constraints)
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

        renderLines.Add(new Text($"{state.SaveHint} SAVE TEXT  Esc CANCEL", new Style(theme.Muted, decoration: Decoration.Dim)));
        return new Panel(new Rows(renderLines))
        {
            Header = new PanelHeader(state.Label.ToUpperInvariant()),
            Border = BoxBorder.Square,
            BorderStyle = new Style(theme.BorderActive),
            Padding = new Padding(1, 0),
            Width = constraints.ClampedWidth,
            Expand = false
        };
    }

    private static IRenderable CreateSelectedLine(
        string line,
        int lineStart,
        MultilineTextBoxState state,
        ControlTheme theme)
    {
        var selectionStart = Math.Clamp(state.SelectionStart - lineStart, 0, line.Length);
        var selectionEnd = Math.Clamp(
            state.SelectionStart + state.SelectionLength - lineStart,
            selectionStart,
            line.Length);
        var textStyle = new Style(theme.Text);
        var selectionStyle = new Style(theme.Surface, theme.AccentBright, Decoration.Bold);
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
