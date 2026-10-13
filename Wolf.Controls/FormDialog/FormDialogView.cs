using Wolf.Controls.TextBox;

namespace Wolf.Controls.FormDialog;

public sealed record FormDialogView(
    IReadOnlyList<FormDialogLine> Lines,
    IReadOnlyList<TextBoxState>? TextBoxes = null,
    int TextBoxWidth = 3);
