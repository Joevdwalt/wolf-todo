namespace WolfTodo.Tui.Controls;

/// <summary>Terminal space made available to a component by its parent layout.</summary>
public sealed record TuiComponentConstraints(int Width, int MaxRows)
{
    public int ClampedWidth => Math.Max(1, Width);

    public int ClampedMaxRows => Math.Max(1, MaxRows);
}
