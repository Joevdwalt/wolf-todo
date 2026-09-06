namespace Wolf.Controls;

/// <summary>State displayed by a keyboard-navigable select list.</summary>
public sealed record SelectListState(
    string Title,
    IReadOnlyList<SelectOption> Options,
    int SelectedIndex,
    string? SearchText,
    string EmptyMessage,
    string Footer,
    string? Error = null)
{
    public int ClampedSelectedIndex => Options.Count == 0 ? -1 : Math.Clamp(SelectedIndex, 0, Options.Count - 1);

    public SelectOption? SelectedOption =>
        ClampedSelectedIndex < 0 ? null : Options[ClampedSelectedIndex];
}
