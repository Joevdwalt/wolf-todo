using System.Collections.Immutable;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public static class ApplicationCommandCatalog
{
    public const string MoveTodoProject = ":move-todo-project";
    public const string RollToday = ":roll-today";

    public static ImmutableArray<string> Create(TuiKeyBindings bindings) =>
    [
        .. new[]
        {
            bindings.QuitCommand,
            bindings.ToggleCompletedCommand,
            bindings.HelpCommand,
            MoveTodoProject,
            RollToday
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
    ];
}
