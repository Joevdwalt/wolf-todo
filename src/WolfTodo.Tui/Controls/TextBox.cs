using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

/// <summary>
/// A reusable, single-line terminal text box with cursor-aware editing.
/// </summary>
public static class TextBox
{
    public static int Height => 4;

    public static TextBoxState Create(bool editable, string text) => new(editable, text, text.Length);

    public static TextBoxTransition Reduce(TextBoxState state, ConsoleKeyInfo key)
    {
        if (!state.Edit)
        {
            return new(state);
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

        var next = key.Key switch
        {
            ConsoleKey.LeftArrow => state with { Cursor = Math.Max(0, cursor - 1) },
            ConsoleKey.RightArrow => state with { Cursor = Math.Min(state.Text.Length, cursor + 1) },
            ConsoleKey.Home => state with { Cursor = 0 },
            ConsoleKey.End => state with { Cursor = state.Text.Length },
            ConsoleKey.Backspace when cursor > 0 => state with
            {
                Text = state.Text.Remove(cursor - 1, 1),
                Cursor = cursor - 1
            },
            ConsoleKey.Delete when cursor < state.Text.Length => state with
            {
                Text = state.Text.Remove(cursor, 1)
            },
            _ when !char.IsControl(key.KeyChar) => state with
            {
                Text = state.Text.Insert(cursor, key.KeyChar.ToString()),
                Cursor = cursor + 1
            },
            _ => state
        };
        return new(next);
    }

    public static IRenderable CreateRenderable(string label, TextBoxState state, TuiTheme theme, int width)
    {
        var outerWidth = Math.Max(3, width);
        var contentWidth = outerWidth - 2;
        var input = CreateInputRenderable(state, theme, contentWidth);
        return new Rows(
            new Text(label.PadRight(outerWidth), new Style(theme.Heading, decoration: Decoration.Bold))
            {
                Justification = Justify.Left
            },
            new Panel(input)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(theme.BorderActive),
                Padding = new Padding(0),
                Width = outerWidth,
                Expand = false
            });
    }

    public static string DisplayText(TextBoxState state, int width)
    {
        var contentWidth = Math.Max(1, width);
        if (!state.Edit)
        {
            return state.Text[..Math.Min(state.Text.Length, contentWidth)].PadRight(contentWidth);
        }

        var display = EditableDisplay(state, contentWidth);
        return (display.Before + display.Cursor + display.After).PadRight(contentWidth);
    }

    private static IRenderable CreateInputRenderable(TextBoxState state, TuiTheme theme, int width)
    {
        var textStyle = new Style(theme.AccentBright, decoration: Decoration.Bold);
        if (!state.Edit)
        {
            return new Text(DisplayText(state, width), textStyle);
        }

        var display = EditableDisplay(state, width);
        return new Columns(
        [
            new Text(display.Before, textStyle),
            new Text(display.Cursor, new Style(theme.Background, theme.AccentBright, Decoration.Bold)),
            new Text(display.After.PadRight(Math.Max(0, width - display.Before.Length - display.Cursor.Length - display.After.Length)), textStyle)
        ])
        {
            Padding = new Padding(0),
            Expand = false
        };
    }

    private static EditableTextDisplay EditableDisplay(TextBoxState state, int width)
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
        return new(before, cursorCharacter, after);
    }
}

internal sealed record EditableTextDisplay(string Before, string Cursor, string After);

public sealed record TextBoxState(bool editing, string Text, int Cursor)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);
    public bool Edit => editing;

}

public sealed record TextBoxTransition(
    TextBoxState? State,
    TextBoxOutcome Outcome = TextBoxOutcome.Editing);

public enum TextBoxOutcome
{
    Editing,
    Accepted,
    Cancelled
}
