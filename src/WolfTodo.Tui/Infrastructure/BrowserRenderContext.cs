using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Infrastructure;

public sealed record BrowserRenderContext(
    int Width,
    int Height,
    bool Compact,
    DateOnly Today,
    SelectListView? SelectList,
    int SelectRows,
    MultilineTextBoxState? TextBox,
    int TextBoxRows,
    TodoTaskEditorDialogView? EditorDialog,
    IReadOnlyList<BrowserStatusLine> StatusLines,
    int ContentHeight);
