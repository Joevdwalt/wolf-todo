using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record ApplicationConfiguration(
    ImmutableArray<string> ProjectFiles,
    TuiKeyBindings KeyBindings)
{
    public ApplicationConfiguration(ImmutableArray<string> projectFiles, string quitCommand)
        : this(projectFiles, TuiKeyBindings.CreateDefaults(quitCommand))
    {
    }

    public string QuitCommand => KeyBindings.QuitCommand;
}
