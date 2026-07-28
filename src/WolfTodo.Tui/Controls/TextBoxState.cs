namespace WolfTodo.Tui.Controls;

public sealed record TextBoxState(string Label, bool editing, string Text, int Cursor, bool IsActive = false)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);

    public bool Edit => editing;
}
