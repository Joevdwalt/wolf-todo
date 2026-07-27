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

public sealed record TextBoxState(string Label, bool editing, string Text, int Cursor, bool IsActive = false)
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
