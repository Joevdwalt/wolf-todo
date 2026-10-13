using WolfTodo.Tui.Features.TaskLinks;
using Spectre.Console.Rendering;
using Wolf.Controls.TextBox;
using Wolf.Controls.TextPrompt;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.TaskLinks.Rendering;

public static class TaskLinkPanelRenderer
{
    public static IRenderable Render(TaskLinkPanelState panel, TuiTheme theme, int width) =>
        TextPrompt.Render(
            new TextPromptView(
                panel.Input,
                panel.IsOpening ? "Enter OPEN · Esc CANCEL" : "Ctrl+A SELECT · Home/End SCROLL · Esc CLOSE",
                panel.Context),
            TuiControlAdapter.ToControlTheme(theme),
            new ControlConstraints(width, TextBox.Height + 2));
}
