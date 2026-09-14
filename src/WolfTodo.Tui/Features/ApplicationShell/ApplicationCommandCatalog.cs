using System.Collections.Immutable;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public static class ApplicationCommandCatalog
{
    public const string TaskLink = ":task-link";
    public const string OpenTask = ":open-task";
    public const string Archive = ":archive";
    public const string MoveTodoProject = ":move-todo-project";
    public const string Pomodoro = ":pomodoro";
    public const string RollToday = ":roll-today";
    public const string DumpScreen = ":dump-screen";
    public const string Configuration = ":config";

    public static ImmutableArray<string> Create(TuiKeyBindings bindings) =>
    [
        .. new[]
        {
            bindings.QuitCommand,
            bindings.ToggleCompletedCommand,
            bindings.HelpCommand,
            Archive,
            MoveTodoProject,
            Pomodoro,
            RollToday,
            DumpScreen,
            Configuration,
            TaskLink,
            OpenTask
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
    ];
}
