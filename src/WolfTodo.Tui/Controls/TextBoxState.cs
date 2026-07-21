namespace WolfTodo.Tui.Controls;

internal sealed record TextBoxState(string Text, int Cursor, bool IsMultiline)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);

    public static TextBoxState Create(string text, bool isMultiline)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        return new TextBoxState(normalized, normalized.Length, isMultiline);
    }
}
