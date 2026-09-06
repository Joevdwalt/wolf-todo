using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls;

/// <summary>A reusable, single-line terminal text box with cursor-aware editing.</summary>
public sealed class TextBox : IControl<TextBoxState, TextBoxOutcome>
{
    public static TextBox Default { get; } = new();

    public static int Height => 2;

    public ControlTransition<TextBoxState, TextBoxOutcome> Reduce(TextBoxState state, ControlInput input)
    {
        if (!state.IsEditable)
        {
            return new(state, TextBoxOutcome.Editing);
        }

        if (input.Kind == ControlInputKind.Cancel)
        {
            return new(null, TextBoxOutcome.Cancelled);
        }

        if (input.Kind == ControlInputKind.Accept)
        {
            return new(state, TextBoxOutcome.Accepted);
        }

        if (input.Kind == ControlInputKind.SelectAll)
        {
            var selected = state.Text.Length == 0
                ? state with { SelectionAnchor = null }
                : state with { Cursor = state.Text.Length, SelectionAnchor = 0 };
            return new(selected, TextBoxOutcome.Editing);
        }

        if (state.HasSelection)
        {
            if (input.Kind is ControlInputKind.MoveLeft or ControlInputKind.MoveHome)
            {
                return new(state with { Cursor = state.SelectionStart, SelectionAnchor = null }, TextBoxOutcome.Editing);
            }

            if (input.Kind is ControlInputKind.MoveRight or ControlInputKind.MoveEnd)
            {
                return new(state with { Cursor = state.SelectionStart + state.SelectionLength, SelectionAnchor = null }, TextBoxOutcome.Editing);
            }

            if (input.Kind is ControlInputKind.Backspace or ControlInputKind.Delete)
            {
                return new(ReplaceSelection(state, string.Empty), TextBoxOutcome.Editing);
            }

            if (input.Kind == ControlInputKind.Character && !char.IsControl(input.Character))
            {
                return new(ReplaceSelection(state, input.Character.ToString()), TextBoxOutcome.Editing);
            }
        }

        var cursor = state.ClampedCursor;
        var next = input.Kind switch
        {
            ControlInputKind.MoveLeft => state with { Cursor = Math.Max(0, cursor - 1), SelectionAnchor = null },
            ControlInputKind.MoveRight => state with { Cursor = Math.Min(state.Text.Length, cursor + 1), SelectionAnchor = null },
            ControlInputKind.MoveHome => state with { Cursor = 0, SelectionAnchor = null },
            ControlInputKind.MoveEnd => state with { Cursor = state.Text.Length, SelectionAnchor = null },
            ControlInputKind.Backspace when cursor > 0 => state with
            {
                Text = state.Text.Remove(cursor - 1, 1), Cursor = cursor - 1, SelectionAnchor = null
            },
            ControlInputKind.Delete when cursor < state.Text.Length => state with
            {
                Text = state.Text.Remove(cursor, 1), SelectionAnchor = null
            },
            ControlInputKind.Character when !char.IsControl(input.Character) => state with
            {
                Text = state.Text.Insert(cursor, input.Character.ToString()), Cursor = cursor + 1, SelectionAnchor = null
            },
            _ => state
        };
        return new(next, TextBoxOutcome.Editing);
    }

    public int Measure(TextBoxState state, ControlConstraints constraints) => Height;

    public IRenderable Render(TextBoxState state, ControlTheme theme, ControlConstraints constraints)
    {
        var outerWidth = Math.Max(3, constraints.ClampedWidth);
        var contentWidth = outerWidth - 2;
        return new Rows(
            new Text(state.Label.PadRight(outerWidth), new Style(theme.Text, decoration: Decoration.Bold)),
            new Panel(CreateInput(state, theme, contentWidth))
            {
                Border = BoxBorder.Square,
                BorderStyle = new Style(state.IsActive ? theme.BorderActive : theme.Border),
                Padding = new Padding(0),
                Width = outerWidth,
                Expand = false
            });
    }

    public static string DisplayText(TextBoxState state, int width)
    {
        var contentWidth = Math.Max(1, width);
        var start = Math.Clamp(state.ClampedCursor - contentWidth + 1, 0, Math.Max(0, state.Text.Length - contentWidth));
        return state.Text.Substring(start, Math.Min(contentWidth, state.Text.Length - start)).PadRight(contentWidth);
    }

    private static IRenderable CreateInput(TextBoxState state, ControlTheme theme, int width)
    {
        var textStyle = new Style(state.IsEditable ? theme.Text : theme.Muted, decoration: Decoration.Bold);
        if (!state.IsEditable)
        {
            return new Text(DisplayText(state, width), textStyle);
        }

        var start = Math.Clamp(state.ClampedCursor - width + 1, 0, Math.Max(0, state.Text.Length - width));
        var visible = state.Text.Substring(start, Math.Min(width, state.Text.Length - start));
        if (state.HasSelection)
        {
            var selectionStart = Math.Clamp(state.SelectionStart - start, 0, visible.Length);
            var selectionEnd = Math.Clamp(state.SelectionStart + state.SelectionLength - start, selectionStart, visible.Length);
            return new Columns(
            [
                new Text(visible[..selectionStart], textStyle),
                new Text(visible[selectionStart..selectionEnd], new Style(theme.Surface, theme.AccentBright, Decoration.Bold)),
                new Text(visible[selectionEnd..].PadRight(width - selectionEnd), textStyle)
            ]) { Padding = new Padding(0), Expand = false };
        }

        var cursorIndex = state.ClampedCursor - start;
        var before = visible[..Math.Clamp(cursorIndex, 0, visible.Length)];
        var cursor = cursorIndex < visible.Length ? visible[cursorIndex].ToString() : " ";
        var after = cursorIndex < visible.Length ? visible[(cursorIndex + 1)..] : string.Empty;
        return new Columns(
        [
            new Text(before, textStyle),
            new Text(cursor, new Style(theme.Surface, theme.AccentBright, Decoration.Bold)),
            new Text(after.PadRight(Math.Max(0, width - before.Length - cursor.Length - after.Length)), textStyle)
        ]) { Padding = new Padding(0), Expand = false };
    }

    private static TextBoxState ReplaceSelection(TextBoxState state, string value)
    {
        var text = state.Text.Remove(state.SelectionStart, state.SelectionLength).Insert(state.SelectionStart, value);
        return state with { Text = text, Cursor = state.SelectionStart + value.Length, SelectionAnchor = null };
    }
}
