using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoBulkEditorState(
    int SelectedIndex,
    string ScheduledDate,
    string Tags,
    string Priority,
    bool Complete,
    int SelectedCount,
    string? Error)
{
    public const int FieldCount = 4;

    internal TextBoxState? FieldTextBox { get; init; }

    public TodoBulkEditorField SelectedField =>
        (TodoBulkEditorField)Math.Clamp(SelectedIndex, 0, FieldCount - 1);

    public static TodoBulkEditorState Create(int selectedCount) => new(
        0,
        string.Empty,
        string.Empty,
        string.Empty,
        false,
        selectedCount,
        null);
}
