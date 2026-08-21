namespace WolfTodo.Tui.Controls;

public sealed record TextBoxState(
    string Label,
    bool editing,
    string Text,
    int Cursor,
    bool IsActive = false,
    int? SelectionAnchor = null)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);

    public int ClampedSelectionAnchor => Math.Clamp(SelectionAnchor ?? ClampedCursor, 0, Text.Length);

    public bool HasSelection => SelectionAnchor.HasValue && ClampedSelectionAnchor != ClampedCursor;

    public int SelectionStart => Math.Min(ClampedSelectionAnchor, ClampedCursor);

    public int SelectionLength => Math.Abs(ClampedCursor - ClampedSelectionAnchor);

    public bool Edit => editing;
}
