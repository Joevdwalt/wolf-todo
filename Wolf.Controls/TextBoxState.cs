namespace Wolf.Controls;

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

    public bool HasSelection => SelectionAnchor is not null && SelectionAnchor.Value != ClampedCursor;

    public int SelectionStart => Math.Min(SelectionAnchor ?? ClampedCursor, ClampedCursor);

    public int SelectionLength => Math.Abs((SelectionAnchor ?? ClampedCursor) - ClampedCursor);

    public static TextBoxState Create(string label, bool isEditable, string text, bool isActive = false) =>
        new(label, isEditable, text, text.Length, isActive);
}
