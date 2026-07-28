namespace WolfTodo.Tui.Controls;

public sealed record TodoTaskEditorDialogView(
    IReadOnlyList<TodoTaskEditorDialogLine> Lines,
    IReadOnlyList<TextBoxState>? TextBoxes = null,
    int TextBoxWidth = 3)
{
    public int Height => TodoTaskEditorDialog.Measure(this, new TuiComponentConstraints(TextBoxWidth, TextBox.Height));
}
