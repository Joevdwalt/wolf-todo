using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

/// <summary>
/// A reusable, single-line terminal text box with cursor-aware editing.
/// </summary>
public sealed class TextBox : ITuiComponent<TextBoxState, TextBoxOutcome>
{
    public static TextBox Default { get; } = new();

    public static int Height => 4;

    public static TextBoxState Create(string label, bool editable, string text, bool isActive = false) =>
        new(label, editable, text, text.Length, isActive);

    public TuiComponentTransition<TextBoxState, TextBoxOutcome> Reduce(
        TextBoxState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (!state.Edit)
        {
            return new(state, TextBoxOutcome.Editing);
        }

        var cursor = state.ClampedCursor;
        if (key.Key == ConsoleKey.Escape)
        {
            return new(null, TextBoxOutcome.Cancelled);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            return new(state, TextBoxOutcome.Accepted);
        }

        if (IsSelectAll(key))
        {
            var selected = state.Text.Length == 0
                ? state with { SelectionAnchor = null }
                : state with { Cursor = state.Text.Length, SelectionAnchor = 0 };
            return new(selected, TextBoxOutcome.Editing);
        }

        if (state.HasSelection)
        {
            if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.Home)
            {
                return new(state with { Cursor = state.SelectionStart, SelectionAnchor = null }, TextBoxOutcome.Editing);
            }

            if (key.Key is ConsoleKey.RightArrow or ConsoleKey.End)
            {
                return new(state with
                {
                    Cursor = state.SelectionStart + state.SelectionLength,
                    SelectionAnchor = null
                }, TextBoxOutcome.Editing);
            }

            if (key.Key is ConsoleKey.Backspace or ConsoleKey.Delete)
            {
                return new(ReplaceSelection(state, string.Empty), TextBoxOutcome.Editing);
            }

            if (!char.IsControl(key.KeyChar))
            {
                return new(ReplaceSelection(state, key.KeyChar.ToString()), TextBoxOutcome.Editing);
            }
        }

        var next = key.Key switch
        {
            ConsoleKey.LeftArrow => state with { Cursor = Math.Max(0, cursor - 1), SelectionAnchor = null },
            ConsoleKey.RightArrow => state with
            {
                Cursor = Math.Min(state.Text.Length, cursor + 1),
                SelectionAnchor = null
            },
            ConsoleKey.Home => state with { Cursor = 0, SelectionAnchor = null },
            ConsoleKey.End => state with { Cursor = state.Text.Length, SelectionAnchor = null },
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
            _ when !char.IsControl(key.KeyChar) => state with
            {
                Text = state.Text.Insert(cursor, key.KeyChar.ToString()),
                Cursor = cursor + 1,
                SelectionAnchor = null
            },
            _ => state
        };
        return new(next, TextBoxOutcome.Editing);
    }

    public int Measure(TextBoxState state, TuiComponentConstraints constraints) => Height;

    public IRenderable Render(TextBoxState state, TuiTheme theme, TuiComponentConstraints constraints)
    {
        var outerWidth = Math.Max(3, constraints.ClampedWidth);
        var contentWidth = outerWidth - 2;
        var input = CreateInputRenderable(state, theme, contentWidth);
        return new Rows(
            new Text(state.Label.PadRight(outerWidth), new Style(Color.White, decoration: Decoration.Bold))
            {
                Justification = Justify.Left
            },
            new Panel(input)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(state.IsActive ? Color.White : theme.BorderActive),
                Padding = new Padding(0),
                Width = outerWidth,
                Expand = false
            });
    }

    public static IRenderable CreateRenderable(TextBoxState state, TuiTheme theme, int width) =>
        Default.Render(state, theme, new TuiComponentConstraints(width, Height));

    public static TextBoxTransition Reduce(TextBoxState state, ConsoleKeyInfo key)
    {
        var transition = Default.Reduce(state, key, TuiKeyBindings.CreateDefaults(":q"));
        return new(transition.State, transition.Outcome);
    }

    public static string DisplayText(TextBoxState state, int width)
    {
        var contentWidth = Math.Max(1, width);
        if (!state.Edit)
        {
            return state.Text[..Math.Min(state.Text.Length, contentWidth)].PadRight(contentWidth);
        }

        if (state.HasSelection)
        {
            return SelectedDisplayText(state, contentWidth);
        }

        var display = EditableDisplay(state, contentWidth);
        return (display.Before + display.Cursor + display.After).PadRight(contentWidth);
    }

    private static IRenderable CreateInputRenderable(TextBoxState state, TuiTheme theme, int width)
    {
        var textColor = !state.Edit ? theme.SecondaryText :
            state.IsActive ? Color.White : theme.AccentBright;
        var textStyle = new Style(textColor, decoration: Decoration.Bold);
        if (!state.Edit)
        {
            return new Text(DisplayText(state, width), textStyle);
        }

        if (state.HasSelection)
        {
            return CreateSelectionRenderable(state, theme, textStyle, width);
        }

        var display = EditableDisplay(state, width);
        return new Columns(
        [
            new Text(display.Before, textStyle),
            new Text(display.Cursor, new Style(theme.Background, textColor, Decoration.Bold)),
            new Text(display.After.PadRight(Math.Max(0, width - display.Before.Length - display.Cursor.Length - display.After.Length)), textStyle)
        ])
        {
            Padding = new Padding(0),
            Expand = false
        };
    }

    private static (string Before, string Cursor, string After) EditableDisplay(TextBoxState state, int width)
    {
        var cursor = state.ClampedCursor;
        var start = Math.Max(0, cursor - width + 1);
        var before = state.Text[start..cursor];
        var cursorCharacter = cursor < state.Text.Length ? state.Text[cursor].ToString() : " ";
        var afterLength = Math.Min(state.Text.Length - cursor - (cursor < state.Text.Length ? 1 : 0),
            Math.Max(0, width - before.Length - 1));
        var after = cursor < state.Text.Length
            ? state.Text.Substring(cursor + 1, afterLength)
            : string.Empty;
        return (before, cursorCharacter, after);
    }

    private static bool IsSelectAll(ConsoleKeyInfo key) =>
        key.Key == ConsoleKey.A && key.Modifiers.HasFlag(ConsoleModifiers.Control);

    private static TextBoxState ReplaceSelection(TextBoxState state, string value)
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

    private static string SelectedDisplayText(TextBoxState state, int width)
    {
        var start = SelectionViewportStart(state, width);
        return state.Text.Substring(start, Math.Min(width, state.Text.Length - start)).PadRight(width);
    }

    private static IRenderable CreateSelectionRenderable(
        TextBoxState state,
        TuiTheme theme,
        Style textStyle,
        int width)
    {
        var start = SelectionViewportStart(state, width);
        var length = Math.Min(width, state.Text.Length - start);
        var visible = state.Text.Substring(start, length);
        var selectionStart = Math.Clamp(state.SelectionStart - start, 0, length);
        var selectionEnd = Math.Clamp(
            state.SelectionStart + state.SelectionLength - start,
            selectionStart,
            length);
        var selectionStyle = new Style(theme.Background, theme.AccentBright, Decoration.Bold);
        return new Columns(
        [
            new Text(visible[..selectionStart], textStyle),
            new Text(visible[selectionStart..selectionEnd], selectionStyle),
            new Text(visible[selectionEnd..].PadRight(width - selectionEnd), textStyle)
        ])
        {
            Padding = new Padding(0),
            Expand = false
        };
    }

    private static int SelectionViewportStart(TextBoxState state, int width) =>
        Math.Clamp(state.ClampedCursor - width, 0, Math.Max(0, state.Text.Length - width));
}
