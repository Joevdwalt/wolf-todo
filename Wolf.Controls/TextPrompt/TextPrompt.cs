using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls.TextBox;

namespace Wolf.Controls.TextPrompt;

/// <summary>A one-field prompt with optional context and a host-supplied footer.</summary>
public static class TextPrompt
{
    public static int Measure(TextPromptView view) => TextBox.TextBox.Height + 1 + (view.Context is null ? 0 : 1);

    public static IRenderable Render(TextPromptView view, ControlTheme theme, ControlConstraints constraints)
    {
        var width = constraints.ClampedWidth;
        var rows = new List<IRenderable>();
        if (view.Context is not null)
        {
            rows.Add(new Text(view.Context, new Style(theme.SecondaryText)).Ellipsis());
        }

        rows.Add(TextBox.TextBox.Default.Render(
            view.Input with { Label = Ellipsize(view.Input.Label, width) },
            theme,
            new ControlConstraints(width, TextBox.TextBox.Height)));
        rows.Add(new Text(view.Footer, new Style(
            view.IsError ? theme.Error : theme.Muted,
            decoration: view.IsError ? Decoration.Bold : view.DimFooter ? Decoration.Dim : Decoration.None)).Ellipsis());
        return new Rows(rows);
    }

    private static string Ellipsize(string value, int maxWidth)
    {
        if (value.GetCellWidth() <= maxWidth) return value;

        var result = new System.Text.StringBuilder();
        var width = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var glyph = rune.ToString();
            var glyphWidth = glyph.GetCellWidth();
            if (width + glyphWidth >= maxWidth) break;
            result.Append(glyph);
            width += glyphWidth;
        }

        return result + "…";
    }
}
