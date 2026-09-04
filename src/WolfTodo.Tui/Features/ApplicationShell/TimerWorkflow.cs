using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class TimerWorkflow(
    WeeklyTimeLogService? weeklyTimeLogService,
    Func<DateTime> nowProvider,
    IPomodoroCompletionNotifier? pomodoroCompletionNotifier,
    ITerminalUi terminalUi)
{
    public ApplicationState Toggle(
        ApplicationState state,
        BrowserView? browser,
        PlannerView? planner,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        bool isTodosTabActive)
    {
        if (configuration.Timer is null)
        {
            return Failure(state, "Task timing requires a [timer] notes_directory configuration.", isTodosTabActive);
        }

        var target = BuildTarget(browser, planner, catalog, isTodosTabActive);
        if (state.Timer is not null)
        {
            var activeWasPomodoro = state.Timer.IsPomodoro;
            var activeIdentity = state.Timer.TodoIdentity;
            if (!state.Timer.IsTaskLinked)
            {
                return state with { Timer = null };
            }

            if (weeklyTimeLogService is null)
            {
                return Failure(state, "Could not write the active task timer.", isTodosTabActive);
            }

            var result = weeklyTimeLogService.Record(
                state.Timer,
                state.Timer.RecordingEnd(nowProvider()),
                configuration.Timer);
            if (!result.Succeeded)
            {
                return Failure(state, result.Error ?? "Could not write task time.", isTodosTabActive);
            }

            state = state with { Timer = null };
            if (activeWasPomodoro || target is null || target.TodoIdentity == activeIdentity)
            {
                return state;
            }
        }

        if (target is null)
        {
            return Failure(state, "Select a todo before starting the timer.", isTodosTabActive);
        }

        if (weeklyTimeLogService is null)
        {
            return Failure(state, "Task timing is unavailable.", isTodosTabActive);
        }

        return state with { Timer = new ActiveTimer(target.TodoIdentity, target.ProjectTitle, target.TodoTitle, nowProvider()) };
    }

    public ApplicationState OpenPomodoroPrompt(
        ApplicationState state,
        BrowserView? browser,
        PlannerView? planner,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        bool untracked,
        bool isTodosTabActive)
    {
        if (configuration.Timer is null)
        {
            return Failure(state, "Pomodoro timing requires a [timer] configuration.", isTodosTabActive);
        }

        if (state.Timer is not null)
        {
            return Failure(state, "Stop the active timer before starting a Pomodoro.", isTodosTabActive);
        }

        var target = untracked ? null : BuildTarget(browser, planner, catalog, isTodosTabActive);
        if (target is not null && weeklyTimeLogService is null)
        {
            return Failure(state, "Task timing is unavailable.", isTodosTabActive);
        }

        var duration = target?.Duration ?? configuration.Timer.PomodoroDuration;
        var label = target is null ? "POMODORO MINUTES" : $"POMODORO MINUTES · {target.TodoTitle}";
        return state with
        {
            PomodoroPrompt = new PomodoroPromptState(
                TextBox.Create(label, true, ((int)duration.TotalMinutes).ToString(System.Globalization.CultureInfo.InvariantCulture), true),
                target?.TodoIdentity,
                target?.ProjectTitle,
                target?.TodoTitle)
        };
    }

    public ApplicationState ReducePrompt(
        ApplicationState state,
        ConsoleKeyInfo key,
        ApplicationConfiguration configuration,
        bool isTodosTabActive)
    {
        var prompt = state.PomodoroPrompt!;
        var transition = TextBox.Default.Reduce(prompt.Input, key, configuration.KeyBindings);
        if (transition.Outcome == TextBoxOutcome.Cancelled)
        {
            return state with { PomodoroPrompt = null };
        }

        var nextInput = transition.State ?? prompt.Input;
        if (transition.Outcome != TextBoxOutcome.Accepted)
        {
            return state with { PomodoroPrompt = prompt with { Input = nextInput, Error = null } };
        }

        if (!int.TryParse(nextInput.Text, out var minutes) || minutes is < 1 or > 960)
        {
            return state with
            {
                PomodoroPrompt = prompt with { Input = nextInput, Error = "Enter a whole number from 1 through 960." }
            };
        }

        var target = prompt.IsTaskLinked
            ? new TimerTarget(prompt.TodoIdentity!, prompt.ProjectTitle!, prompt.TodoTitle!, null)
            : null;
        return StartPomodoro(state with { PomodoroPrompt = null }, target, TimeSpan.FromMinutes(minutes), configuration, isTodosTabActive);
    }

    public ApplicationState StartPomodoroCommand(
        ApplicationState state,
        BrowserView? browser,
        PlannerView? planner,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ApplicationCommandTransition command,
        bool isTodosTabActive)
    {
        if (configuration.Timer is null)
        {
            return Failure(state, "Pomodoro timing requires a [timer] configuration.", isTodosTabActive);
        }

        if (state.Timer is not null)
        {
            return Failure(state, "Stop the active timer before starting a Pomodoro.", isTodosTabActive);
        }

        var selectedTarget = BuildTarget(browser, planner, catalog, isTodosTabActive);
        if (command.PomodoroDurationSource == PomodoroDurationSource.SelectedTask)
        {
            if (selectedTarget is null)
            {
                return Failure(state, "Select a todo with a duration before using :pomodoro task.", isTodosTabActive);
            }

            if (selectedTarget.Duration is null)
            {
                return Failure(state, "The selected todo has no ⏱ duration.", isTodosTabActive);
            }

            return StartPomodoro(state, selectedTarget, selectedTarget.Duration.Value, configuration, isTodosTabActive);
        }

        var duration = TimeSpan.FromMinutes(command.PomodoroMinutes!.Value);
        return StartPomodoro(state, command.PomodoroUntracked ? null : selectedTarget, duration, configuration, isTodosTabActive);
    }

    public ApplicationState CompletePomodoro(ApplicationState state, ApplicationConfiguration configuration, bool isTodosTabActive)
    {
        if (state.Timer is not { IsPomodoro: true, CompletionHandled: false } timer || !timer.IsComplete(nowProvider()))
        {
            return state;
        }

        state = state with { Timer = timer with { CompletionHandled = true } };
        var completion = new PomodoroCompletion(timer.TodoTitle, timer.Duration ?? TimeSpan.Zero, nowProvider());
        pomodoroCompletionNotifier?.Notify(completion, configuration.Timer?.Bell != false);
        if (pomodoroCompletionNotifier is null && configuration.Timer?.Bell != false)
        {
            terminalUi.RingBell();
        }

        return Stop(state, configuration, isTodosTabActive) with { PomodoroCompletion = completion };
    }

    public ApplicationState Stop(ApplicationState state, ApplicationConfiguration configuration, bool isTodosTabActive)
    {
        if (state.Timer is null) return state;
        if (!state.Timer.IsTaskLinked) return state with { Timer = null };
        if (configuration.Timer is null || weeklyTimeLogService is null)
            return Failure(state, "Could not write the active task timer.", isTodosTabActive);
        var result = weeklyTimeLogService.Record(state.Timer, state.Timer.RecordingEnd(nowProvider()), configuration.Timer);
        return result.Succeeded
            ? state with { Timer = null }
            : Failure(state, result.Error ?? "Could not write task time.", isTodosTabActive);
    }

    public string? Status(ActiveTimer? timer)
    {
        if (timer is null) return null;
        if (timer.IsPomodoro)
        {
            var totalSeconds = (int)Math.Ceiling(timer.Remaining(nowProvider()).TotalSeconds);
            var countdown = totalSeconds >= 3600
                ? $"{totalSeconds / 3600:00}:{totalSeconds % 3600 / 60:00}:{totalSeconds % 60:00}"
                : $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            return $"POMODORO {countdown}{(timer.TodoTitle is null ? string.Empty : $" · {timer.TodoTitle}")}";
        }

        var elapsed = timer.Elapsed(nowProvider());
        return $"TIMER {((int)elapsed.TotalHours):00}:{elapsed.Minutes:00} · {timer.TodoTitle}";
    }

    public bool IsBright(ActiveTimer? timer) => timer is not null && nowProvider().Second % 2 == 0;

    public PlannerFocusBlock? ActiveFocusBlock(ActiveTimer? timer) =>
        timer is { IsPomodoro: true, EndsAt: { } endsAt }
            ? new PlannerFocusBlock(timer.StartedAt, endsAt, timer.TodoTitle)
            : null;

    private ApplicationState StartPomodoro(
        ApplicationState state,
        TimerTarget? target,
        TimeSpan duration,
        ApplicationConfiguration configuration,
        bool isTodosTabActive)
    {
        if (configuration.Timer is null)
        {
            return Failure(state, "Pomodoro timing requires a [timer] configuration.", isTodosTabActive);
        }

        if (state.Timer is not null)
        {
            return Failure(state, "Stop the active timer before starting a Pomodoro.", isTodosTabActive);
        }

        if (target is not null && weeklyTimeLogService is null)
        {
            return Failure(state, "Task timing is unavailable.", isTodosTabActive);
        }

        return state with
        {
            Timer = new ActiveTimer(target?.TodoIdentity, target?.ProjectTitle, target?.TodoTitle, nowProvider(), duration),
            PomodoroPrompt = null
        };
    }

    private static TimerTarget? BuildTarget(BrowserView? browser, PlannerView? planner, ProjectCatalog catalog, bool isTodosTabActive)
    {
        if (isTodosTabActive && browser?.SelectedTodoIdentity is { } identity && browser.SelectedTodo is { } todo)
        {
            var project = catalog.Projects.FirstOrDefault(candidate => candidate.Path == identity.ProjectPath);
            return project is null ? null : new TimerTarget(identity, project.Title, todo.Title, todo.Duration);
        }

        return !isTodosTabActive && planner?.SelectedFocusedAssignment is { } assignment
            ? new TimerTarget(assignment.Identity, assignment.ProjectTitle, assignment.Todo.Title, assignment.Todo.Duration)
            : null;
    }

    private static ApplicationState Failure(ApplicationState state, string error, bool isTodosTabActive) => isTodosTabActive
        ? state with { Browser = state.Browser with { Error = error } }
        : state with { Planner = state.Planner with { Error = error } };

    private sealed record TimerTarget(TodoIdentity TodoIdentity, string ProjectTitle, string TodoTitle, TimeSpan? Duration);
}
