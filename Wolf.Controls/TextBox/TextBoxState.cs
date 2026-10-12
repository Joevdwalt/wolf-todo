namespace Wolf.Controls.TextBox;

/// <summary>State for a single-line, cursor-aware text box.</summary>
public sealed record TextBoxState(
    string Label,
    bool IsEditable,
    string Text,
    int Cursor,
    bool IsActive = false,
    int? SelectionAnchor = null)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);

    public int ClampedSelectionAnchor => Math.Clamp(SelectionAnchor ?? ClampedCursor, 0, Text.Length);

    public bool HasSelection => SelectionAnchor is not null && ClampedSelectionAnchor != ClampedCursor;

    public int SelectionStart => Math.Min(ClampedSelectionAnchor, ClampedCursor);

    public int SelectionLength => Math.Abs(ClampedSelectionAnchor - ClampedCursor);

    public static TextBoxState Create(string label, bool isEditable, string text, bool isActive = false) =>
        new(label, isEditable, text, text.Length, isActive);
}
