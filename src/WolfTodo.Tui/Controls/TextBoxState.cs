namespace WolfTodo.Tui.Controls;

internal sealed record MultilineTextBoxState(string Text, int Cursor, bool IsMultiline)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);

    public static MultilineTextBoxState Create(string text, bool isMultiline)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        return new MultilineTextBoxState(normalized, normalized.Length, isMultiline);
    }
}
