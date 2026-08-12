using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ApplicationShell.Rendering;

public static class PomodoroPromptRenderer
{
    public static int Height => TextBox.Height + 1;

    public static IRenderable Render(PomodoroPromptState prompt, TuiTheme theme, int width)
    {
        var footer = prompt.Error is null
            ? "Enter START · Esc CANCEL"
            : $"{prompt.Error} · Enter START · Esc CANCEL";
        var footerStyle = new Style(
            prompt.Error is null ? theme.Muted : theme.Error,
            decoration: prompt.Error is null ? Decoration.Dim : Decoration.Bold);

        return new Rows(
            TextBox.Default.Render(
                prompt.Input with { Label = Ellipsize(prompt.Input.Label, Math.Max(1, width)) },
                theme,
                new TuiComponentConstraints(width, TextBox.Height)),
            new Text(footer, footerStyle).Ellipsis());
    }

    private static string Ellipsize(string value, int maxWidth)
    {
        if (value.GetCellWidth() <= maxWidth)
        {
            return value;
        }

        var result = new System.Text.StringBuilder();
        var width = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var text = rune.ToString();
            var runeWidth = text.GetCellWidth();
            if (width + runeWidth >= maxWidth)
            {
                break;
            }

            result.Append(text);
            width += runeWidth;
        }

        return result + "…";
    }
}
