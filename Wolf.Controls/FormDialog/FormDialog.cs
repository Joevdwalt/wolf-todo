using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls.TextBox;

namespace Wolf.Controls.FormDialog;

/// <summary>A bordered form layout; the host supplies fields, messages, and input behavior.</summary>
public static class FormDialog
{
    public static int Measure(FormDialogView view, ControlConstraints constraints) =>
        view.Lines.Count + 2 + (view.TextBoxes ?? []).Sum(textBox => TextBox.TextBox.Default.Measure(textBox, constraints));

    public static IRenderable Render(FormDialogView view, ControlTheme theme)
    {
        var rows = view.Lines.Select(line => (IRenderable)new Text(line.Text, Style(line.Role, theme))).ToList();
        rows.InsertRange(Math.Min(1, rows.Count), (view.TextBoxes ?? []).Select(textBox =>
            TextBox.TextBox.Default.Render(textBox, theme, new ControlConstraints(view.TextBoxWidth, TextBox.TextBox.Height))));
        return new Panel(new Rows(rows))
        {
            Border = BoxBorder.Square,
            BorderStyle = new Style(theme.BorderActive),
            Expand = true
        };
    }

    private static Style Style(FormDialogRole role, ControlTheme theme) => role switch
    {
        FormDialogRole.Label => new(theme.Heading, decoration: Decoration.Bold),
        FormDialogRole.Value => new(theme.SecondaryText),
        FormDialogRole.ActiveValue => new(theme.AccentBright, decoration: Decoration.Bold),
        FormDialogRole.Placeholder or FormDialogRole.Hint => new(theme.Muted, decoration: Decoration.Dim),
        FormDialogRole.Error => new(theme.Error, decoration: Decoration.Bold),
        FormDialogRole.Warning => new(theme.Warning, decoration: Decoration.Bold),
        _ => new(theme.Text)
    };
}
