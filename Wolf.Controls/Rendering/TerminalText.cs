using System.Globalization;
using System.Text;
using Spectre.Console;

namespace Wolf.Controls.Rendering;

/// <summary>Fits plain text to terminal cells without splitting Unicode text elements.</summary>
public static class TerminalText
{
    public static string Clip(string value, int width)
    {
        if (width <= 0) return string.Empty;
        if (value.GetCellWidth() <= width) return value;

        var result = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            var elementWidth = element.GetCellWidth();
            if (elementWidth > width) break;
            result.Append(element);
            width -= elementWidth;
        }
        return result.ToString();
    }

    public static string Ellipsize(string value, int width)
    {
        if (width <= 0) return string.Empty;
        return value.GetCellWidth() <= width ? value : Clip(value, width - 1) + "…";
    }

    public static string PadColumn(string value, int width)
    {
        var fitted = Ellipsize(value, width);
        return fitted + new string(' ', Math.Max(0, width - fitted.GetCellWidth()));
    }
}
