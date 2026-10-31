namespace Wolf.Controls.Rendering;

/// <summary>Selects a window of logical rows ending at or after the selected row.</summary>
public static class ListViewport
{
    public static IReadOnlyList<T> Fit<T>(IReadOnlyList<T> rows, int height, int selectedIndex)
    {
        if (height <= 0) return Array.Empty<T>();
        if (rows.Count <= height) return rows;
        var start = (int)Math.Clamp((long)selectedIndex - height + 1, 0, rows.Count - height);
        return rows.Skip(start).Take(height).ToArray();
    }
}
