namespace WolfTodo.Tui.Controls;

internal static class TextBoxReducer
{
    public static TextBoxState Reduce(TextBoxState state, ConsoleKeyInfo key)
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

    private static TextBoxState Insert(TextBoxState state, string value)
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
}
