using Spectre.Console;
using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Infrastructure;

public sealed class SurfaceThemeRenderer
{
    public Style Style(
        Color color,
        Decoration decoration = Decoration.None,
        Color? background = null) =>
        new(color, background ?? Color.Default, decoration);

    public IRenderable OnSurface(IRenderable content, Color background, bool expand = false) =>
        background == Color.Default
            ? content
            : new SurfaceRenderable(content, background, expand);

    public void AppendStyled(
        System.Text.StringBuilder output,
        string value,
        Color color,
        Decoration decoration = Decoration.None,
        Color? background = null)
    {
        if (value.Length == 0)
        {
            return;
        }

        var styles = new List<string>();
        if (color != Color.Default)
        {
            styles.Add(color.ToMarkup());
        }

        if (background is not null && background != Color.Default)
        {
            styles.Add($"on {background.Value.ToMarkup()}");
        }

        if ((decoration & Decoration.Bold) != 0)
        {
            styles.Add("bold");
        }

        if ((decoration & Decoration.Dim) != 0)
        {
            styles.Add("dim");
        }

        var content = Markup.Escape(value);
        if (styles.Count == 0)
        {
            output.Append(content);
            return;
        }

        output.Append('[');
        output.AppendJoin(' ', styles);
        output.Append(']');
        output.Append(content);
        output.Append("[/]");
    }
}
