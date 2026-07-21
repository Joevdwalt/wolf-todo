using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Controls;

internal static class TuiControlPanel
{
    public static IRenderable Create(string title, IRenderable content, TuiTheme theme) =>
        new SurfaceRenderable(
            new Panel(content)
            {
                Header = new PanelHeader(title.ToUpperInvariant()),
                Border = BoxBorder.Square,
                BorderStyle = new Style(theme.BorderActive),
                Expand = true
            },
            theme.Surface2,
            true);
}
