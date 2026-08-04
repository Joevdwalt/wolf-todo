using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Rendering;

public static class TerminalLayout
{
    public static int SafeWindowWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            return 100;
        }
    }

    public static int SafeWindowHeight()
    {
        try
        {
            return Console.WindowHeight;
        }
        catch (IOException)
        {
            return 30;
        }
    }

    public static int SelectListRows(int terminalHeight) =>
        Math.Clamp(terminalHeight / 5, 3, 7);

    public static int TextBoxRows(int terminalHeight) =>
        Math.Clamp(terminalHeight / 4, 3, 8);

    public static int AvailableContentHeight(int terminalHeight, int statusLineCount)
    {
        const int tabTableStatusBorderAndCursorHeight = 8;
        return Math.Max(1, terminalHeight - tabTableStatusBorderAndCursorHeight - statusLineCount);
    }

    public static int? DialogContentHeight(TodoTaskEditorDialogView? dialog) =>
        dialog is null ? null : dialog.Height - 2;

    public static int PickerHeight(
        SelectListView? selectList,
        int width,
        int selectRows,
        MultilineTextBoxState? textBox,
        int textBoxRows) =>
        selectList is not null
            ? SelectList.Default.Measure(selectList, new TuiComponentConstraints(width, selectRows))
            : textBox is not null
                ? MultilineTextBox.Default.Measure(textBox, new TuiComponentConstraints(width, textBoxRows))
                : 0;
}
