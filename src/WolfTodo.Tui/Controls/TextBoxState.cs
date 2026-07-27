namespace WolfTodo.Tui.Controls;

public sealed record MultilineTextBoxState(string Label, string Text, int Cursor, bool IsMultiline)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);

    public static MultilineTextBoxState Create(string label, string text, bool isMultiline)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        return new MultilineTextBoxState(label, normalized, normalized.Length, isMultiline);
    }
}
