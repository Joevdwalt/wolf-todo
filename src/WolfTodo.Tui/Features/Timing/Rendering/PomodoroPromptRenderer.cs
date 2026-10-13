using WolfTodo.Tui.Features.Timing;
using Spectre.Console.Rendering;
using Wolf.Controls.TextBox;
using Wolf.Controls.TextPrompt;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.Timing.Rendering;

public static class PomodoroPromptRenderer
{
    public static int Height => TextBox.Height + 1;

    public static IRenderable Render(PomodoroPromptState prompt, TuiTheme theme, int width)
    {
        var footer = prompt.Error is null
            ? "Enter START · Esc CANCEL"
            : $"{prompt.Error} · Enter START · Esc CANCEL";
        return TextPrompt.Render(
            new TextPromptView(prompt.Input, footer, IsError: prompt.Error is not null, DimFooter: true),
            TuiControlAdapter.ToControlTheme(theme),
            new ControlConstraints(width, Height));
    }
}
