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
                    Error = null
                })
                : new ApplicationCommandTransition(state);
        }

        if (key.Key == ConsoleKey.Escape)
        {
            return new ApplicationCommandTransition(state with
            {
                IsActive = false,
                Value = string.Empty,
                Error = null
            });
        }

        if (key.Key == ConsoleKey.Enter)
        {
            const string movePrefix = ":move-todo-project";
            if (state.Value.StartsWith(movePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var projectTitle = state.Value[movePrefix.Length..].Trim();
                return new ApplicationCommandTransition(state with
                {
                    IsActive = false,
                    Value = string.Empty,
                    Error = projectTitle.Length == 0
                        ? "Usage: :move-todo-project <project title>"
                        : null
                }, projectTitle.Length == 0 ? ApplicationCommandOperation.None : ApplicationCommandOperation.MoveTodoProject,
                projectTitle.Length == 0 ? null : projectTitle);
            }

            var operation = state.Value switch
            {
                var command when command == bindings.QuitCommand => ApplicationCommandOperation.Exit,
                var command when command == bindings.ToggleCompletedCommand =>
                    ApplicationCommandOperation.ToggleCompleted,
                var command when command == bindings.HelpCommand => ApplicationCommandOperation.OpenPalette,
                _ => ApplicationCommandOperation.None
            };
            return new ApplicationCommandTransition(state with
            {
                IsActive = false,
                Value = string.Empty,
                Error = operation == ApplicationCommandOperation.None
                    ? $"Unknown command: {state.Value}"
                    : null
            }, operation);
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            return new ApplicationCommandTransition(state with
            {
                Value = state.Value.Length > 1 ? state.Value[..^1] : state.Value,
                Error = null
            });
        }

        return char.IsControl(key.KeyChar)
            ? new ApplicationCommandTransition(state)
            : new ApplicationCommandTransition(state with
            {
                Value = state.Value + key.KeyChar,
                Error = null
            });
    }
}
