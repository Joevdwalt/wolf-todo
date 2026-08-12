using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record PomodoroPromptState(
    TextBoxState Input,
    TodoIdentity? TodoIdentity,
    string? ProjectTitle,
    string? TodoTitle,
    string? Error = null)
{
    public bool IsTaskLinked => TodoIdentity is not null && ProjectTitle is not null && TodoTitle is not null;
}
