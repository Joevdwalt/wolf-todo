namespace WolfTodo.Tui.Controls;

public sealed record MultilineTextBoxState(
    string Label,
    string Text,
    int Cursor,
    bool IsMultiline,
    int? SelectionAnchor = null)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);

    public int ClampedSelectionAnchor => Math.Clamp(SelectionAnchor ?? ClampedCursor, 0, Text.Length);

    public bool HasSelection => SelectionAnchor.HasValue && ClampedSelectionAnchor != ClampedCursor;

    public int SelectionStart => Math.Min(ClampedSelectionAnchor, ClampedCursor);

    public int SelectionLength => Math.Abs(ClampedCursor - ClampedSelectionAnchor);

    public static MultilineTextBoxState Create(string label, string text, bool isMultiline)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        return new MultilineTextBoxState(label, normalized, normalized.Length, isMultiline);
    }
}
