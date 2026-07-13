using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record ApplicationConfiguration(
    ImmutableArray<string> ProjectFiles,
    BrowserKeyBindings KeyBindings)
{
    public ApplicationConfiguration(ImmutableArray<string> projectFiles, string quitCommand)
        : this(projectFiles, BrowserKeyBindings.CreateDefaults(quitCommand))
    {
    }

    public string QuitCommand => KeyBindings.QuitCommand;
}
