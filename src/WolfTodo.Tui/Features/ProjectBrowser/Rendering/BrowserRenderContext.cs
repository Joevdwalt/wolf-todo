using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;

namespace WolfTodo.Tui.Features.ProjectBrowser.Rendering;

public sealed record BrowserRenderContext(
    int Width,
    int Height,
    bool Compact,
    DateOnly Today,
    SelectListView? SelectList,
    int SelectRows,
    MultilineTextBoxState? TextBox,
    int TextBoxRows,
    PomodoroPromptState? PomodoroPrompt,
    TodoTaskEditorDialogView? EditorDialog,
    IReadOnlyList<BrowserStatusLine> StatusLines,
    int ContentHeight);
