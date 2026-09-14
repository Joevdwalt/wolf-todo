using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ApplicationShell.Rendering;

public static class TaskLinkPanelRenderer
{
    public static IRenderable Render(TaskLinkPanelState panel, TuiTheme theme, int width) =>
        new Rows(
            new Text(panel.Context, new Style(theme.SecondaryText)).Ellipsis(),
            TextBox.Default.Render(panel.Input, theme, new TuiComponentConstraints(width, TextBox.Height)),
            new Text(panel.IsOpening
                ? "Enter OPEN · Esc CANCEL"
                : "Ctrl+A SELECT · Home/End SCROLL · Esc CLOSE", new Style(theme.Muted)).Ellipsis());
}
