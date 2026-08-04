using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Infrastructure;

public sealed record PlannerRenderContext(
    int Width,
    int Height,
    SelectListView? SelectList,
    int SelectRows,
    MultilineTextBoxState? TextBox,
    int TextBoxRows,
    TodoTaskEditorDialogView? EditorDialog,
    IReadOnlyList<BrowserStatusLine> Status,
    bool WideSidePanels,
    bool ShowAllDayPanel,
    bool CompactDetails,
    int NarrowAllDayHeight,
    int AvailableRows,
    int TimelineWidth);
