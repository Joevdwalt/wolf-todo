namespace Wolf.Controls;

/// <summary>Terminal space made available to a control by its host layout.</summary>
public sealed record ControlConstraints(int Width, int MaxRows)
{
    public int ClampedWidth => Math.Max(1, Width);

    public int ClampedMaxRows => Math.Max(1, MaxRows);
}
