namespace Wolf.Controls.Rendering;

/// <summary>A fixed cell size or a weighted share of the remaining layout space.</summary>
public readonly record struct LayoutSize(int Value, bool IsWeighted)
{
    public static LayoutSize Fixed(int cells) => new(Math.Max(0, cells), false);
    public static LayoutSize Weighted(int weight = 1) => new(Math.Max(1, weight), true);
}
