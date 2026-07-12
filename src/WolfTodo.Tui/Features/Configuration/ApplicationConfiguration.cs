using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record ApplicationConfiguration(
    ImmutableArray<string> ProjectFiles,
    string QuitCommand);
