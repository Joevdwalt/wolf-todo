namespace WolfTodo.Tui.Controls;

public sealed record SelectListView(
    string Title,
    IReadOnlyList<SelectOption> Options,
    int SelectedIndex,
    string? SearchText,
    string EmptyMessage,
    string Footer,
    string? Error = null)
{
    public int ClampedSelectedIndex => Options.Count == 0
        ? 0
        : Math.Clamp(SelectedIndex, 0, Options.Count - 1);
}
