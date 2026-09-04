using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationCommandReducer
{
    public ApplicationCommandTransition Reduce(
        ApplicationCommandState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings)
    {
        if (!state.IsActive)
        {
            return bindings.MatchesCommandMode(key)
                ? new ApplicationCommandTransition(state with
                {
                    IsActive = true,
                    Value = ":",
                    Error = null,
                    CompletionSeed = null,
                    CompletionIndex = -1
                })
                : new ApplicationCommandTransition(state);
        }

        if (key.Key == ConsoleKey.Escape)
        {
            return new ApplicationCommandTransition(ApplicationCommandState.Initial);
        }

        if (key.Key == ConsoleKey.Tab)
        {
            return Complete(state, bindings);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            if (state.Value.Equals(ApplicationCommandCatalog.Pomodoro, StringComparison.OrdinalIgnoreCase) ||
                state.Value.StartsWith(ApplicationCommandCatalog.Pomodoro + " ", StringComparison.OrdinalIgnoreCase))
            {
                return ParsePomodoro(state.Value);
            }

            if (state.Value.Equals(
                    ApplicationCommandCatalog.MoveTodoProject,
                    StringComparison.OrdinalIgnoreCase) ||
                state.Value.StartsWith(
                    ApplicationCommandCatalog.MoveTodoProject + " ",
                    StringComparison.OrdinalIgnoreCase))
            {
                var projectTitle =
                    state.Value[ApplicationCommandCatalog.MoveTodoProject.Length..].Trim();
                return new ApplicationCommandTransition(
                    Closed(projectTitle.Length == 0
                        ? "Usage: :move-todo-project <project title>"
                        : null),
                    projectTitle.Length == 0
                        ? ApplicationCommandOperation.None
                        : ApplicationCommandOperation.MoveTodoProject,
                    projectTitle.Length == 0 ? null : projectTitle);
            }

            var operation = state.Value switch
            {
                var command when command == bindings.QuitCommand => ApplicationCommandOperation.Exit,
                var command when command == bindings.ToggleCompletedCommand =>
                    ApplicationCommandOperation.ToggleCompleted,
                var command when command == bindings.HelpCommand => ApplicationCommandOperation.OpenPalette,
                var command when command.Equals(
                    ApplicationCommandCatalog.Archive,
                    StringComparison.OrdinalIgnoreCase) =>
                    ApplicationCommandOperation.ArchiveCompleted,
                var command when command.Equals(
                    ApplicationCommandCatalog.RollToday,
                    StringComparison.OrdinalIgnoreCase) =>
                    ApplicationCommandOperation.RollProjectToday,
                var command when command.Equals(
                    ApplicationCommandCatalog.DumpScreen,
                    StringComparison.OrdinalIgnoreCase) =>
                    ApplicationCommandOperation.DumpScreen,
                _ => ApplicationCommandOperation.None
            };
            return new ApplicationCommandTransition(
                Closed(operation == ApplicationCommandOperation.None
                    ? $"Unknown command: {state.Value}"
                    : null),
                operation);
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            return new ApplicationCommandTransition(state with
            {
                Value = state.Value.Length > 1 ? state.Value[..^1] : state.Value,
                Error = null,
                CompletionSeed = null,
                CompletionIndex = -1
            });
        }

        return char.IsControl(key.KeyChar)
            ? new ApplicationCommandTransition(state)
            : new ApplicationCommandTransition(state with
            {
                Value = state.Value + key.KeyChar,
                Error = null,
                CompletionSeed = null,
                CompletionIndex = -1
            });
    }

    private static ApplicationCommandTransition Complete(
        ApplicationCommandState state,
        TuiKeyBindings bindings)
    {
        var seed = state.CompletionSeed ?? state.Value;
        var matches = ApplicationCommandCatalog.Create(bindings)
            .Where(command => command.StartsWith(seed, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0)
        {
            return new ApplicationCommandTransition(state);
        }

        var index = state.CompletionSeed is null
            ? 0
            : (state.CompletionIndex + 1) % matches.Length;
        return new ApplicationCommandTransition(state with
        {
            Value = matches[index],
            Error = null,
            CompletionSeed = matches.Length > 1 ? seed : null,
            CompletionIndex = matches.Length > 1 ? index : -1
        });
    }

    private static ApplicationCommandState Closed(string? error) =>
        ApplicationCommandState.Initial with { Error = error };

    private static ApplicationCommandTransition ParsePomodoro(string value)
    {
        const string usage = "Usage: :pomodoro <minutes|task> [--untracked]";
        var arguments = value[ApplicationCommandCatalog.Pomodoro.Length..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var untracked = arguments.Any(argument =>
            argument.Equals("--untracked", StringComparison.OrdinalIgnoreCase));
        var durationArguments = arguments
            .Where(argument => !argument.Equals("--untracked", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (arguments.Count(argument => argument.Equals("--untracked", StringComparison.OrdinalIgnoreCase)) > 1 ||
            durationArguments.Length != 1)
        {
            return new ApplicationCommandTransition(Closed(usage));
        }

        if (durationArguments[0].Equals("task", StringComparison.OrdinalIgnoreCase))
        {
            return untracked
                ? new ApplicationCommandTransition(Closed("The task duration cannot be used with --untracked."))
                : new ApplicationCommandTransition(
                    Closed(null),
                    ApplicationCommandOperation.StartPomodoro,
                    PomodoroDurationSource: PomodoroDurationSource.SelectedTask);
        }

        if (!int.TryParse(durationArguments[0], out var minutes) || minutes is < 1 or > 960)
        {
            return new ApplicationCommandTransition(Closed("Pomodoro minutes must be a whole number from 1 through 960."));
        }

        return new ApplicationCommandTransition(
            Closed(null),
            ApplicationCommandOperation.StartPomodoro,
            PomodoroDurationSource: PomodoroDurationSource.ExplicitMinutes,
            PomodoroMinutes: minutes,
            PomodoroUntracked: untracked);
    }
}
