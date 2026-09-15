using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationCommandDispatcher(
    ApplicationCommandReducer commandReducer,
    TimerWorkflow timerWorkflow,
    BrowserWorkflow browserWorkflow,
    FocusedTaskWorkflow focusedTaskWorkflow,
    TaskLinkWorkflow taskLinkWorkflow,
    BrowserReducer browserReducer,
    ITerminalUi terminalUi,
    IExternalEditorLauncher? externalEditorLauncher,
    ProjectTodoMutationService? mutationService)
{
    public ApplicationInputResult Handle(ApplicationInputContext context, ConsoleKeyInfo key)
    {
        var transition = commandReducer.Reduce(
            context.State.Command,
            key,
            context.Configuration.KeyBindings);
        var state = context.State with { Command = transition.State };
        var catalog = context.Catalog;
        if (transition.Operation == ApplicationCommandOperation.Exit)
        {
            state = timerWorkflow.Stop(
                state,
                context.Configuration,
                state.Tabs.ActiveTab.Value == "todos");
            return new ApplicationInputResult(state, catalog, state.Timer is null);
        }

        if (transition.Operation == ApplicationCommandOperation.ToggleCompleted)
        {
            state = state.FocusedTask is not null
                ? state with
                {
                    Command = state.Command with
                    {
                        Error = "Exit focus mode to change list visibility."
                    }
                }
                : state with
                {
                    Browser = state.Browser with
                    {
                        ShowCompleted = !state.Browser.ShowCompleted,
                        TodoIndex = 0,
                        PendingTodoSelection = null,
                        Error = null
                    }
                };
        }

        if (transition.Operation == ApplicationCommandOperation.OpenPalette)
        {
            state = state with { Palette = CommandPaletteState.Closed with { IsOpen = true } };
        }

        if (transition.Operation == ApplicationCommandOperation.MoveTodoProject)
        {
            var moveResult = context.FocusedTaskView is not null
                ? focusedTaskWorkflow.MoveSelectedToProject(
                    state,
                    context.FocusedTaskView,
                    transition.ProjectTitle,
                    catalog,
                    context.Configuration,
                    mutationService)
                : browserWorkflow.MoveSelectedTodoToProject(
                    state,
                    context.BrowserView,
                    transition.ProjectTitle,
                    catalog,
                    context.Configuration,
                    mutationService,
                    state.Tabs.ActiveTab.Value == "todos");
            state = moveResult.State;
            catalog = moveResult.Catalog;
        }

        if (transition.Operation == ApplicationCommandOperation.ArchiveCompleted)
        {
            if (state.FocusedTask is not null)
            {
                state = state with
                {
                    Command = state.Command with
                    {
                        Error = "Exit focus mode before archiving a project."
                    }
                };
                return new ApplicationInputResult(state, catalog);
            }

            var archiveResult = browserWorkflow.ArchiveCompletedProject(
                state,
                context.BrowserView,
                catalog,
                context.Configuration,
                mutationService,
                state.Tabs.ActiveTab.Value == "todos");
            state = archiveResult.State;
            catalog = archiveResult.Catalog;
        }

        if (transition.Operation == ApplicationCommandOperation.RollProjectToday)
        {
            if (state.FocusedTask is not null ||
                state.Tabs.ActiveTab.Value != "todos" ||
                context.BrowserView is null)
            {
                state = state with
                {
                    Command = state.Command with
                    {
                        Error = "Open Todos and select a project before rolling tasks to today."
                    }
                };
            }
            else
            {
                var browserTransition = browserReducer.ReduceAction(
                    state.Browser,
                    BrowserAction.RollProjectToday,
                    context.BrowserView);
                var rollResult = browserWorkflow.ApplyTransition(
                    state,
                    browserTransition,
                    catalog,
                    context.Configuration,
                    mutationService);
                state = rollResult.State;
                catalog = rollResult.Catalog;
            }
        }

        if (transition.Operation == ApplicationCommandOperation.StartPomodoro)
        {
            state = context.FocusedTaskView is not null
                ? timerWorkflow.StartPomodoroCommandFocused(
                    state,
                    context.FocusedTaskView,
                    context.Configuration,
                    transition,
                    state.Tabs.ActiveTab.Value == "todos")
                : timerWorkflow.StartPomodoroCommand(
                    state,
                    context.BrowserView,
                    context.PlannerView,
                    catalog,
                    context.Configuration,
                    transition,
                    state.Tabs.ActiveTab.Value == "todos");
        }

        if (transition.Operation == ApplicationCommandOperation.DumpScreen)
        {
            var dump = terminalUi.DumpScreen();
            state = state with
            {
                Command = state.Command with
                {
                    Error = dump.Succeeded
                        ? $"Screen dumped to {dump.Path}"
                        : dump.Error
                }
            };
        }

        if (transition.Operation == ApplicationCommandOperation.GenerateTaskLink)
        {
            state = taskLinkWorkflow.Generate(
                state,
                catalog,
                context.BrowserView,
                context.PlannerView,
                context.FocusedTaskView);
        }

        if (transition.Operation == ApplicationCommandOperation.OpenTaskLink)
        {
            state = taskLinkWorkflow.Open(
                state,
                catalog,
                transition.TaskCode!,
                context.Configuration.SidebarItems.Length);
        }

        if (transition.Operation == ApplicationCommandOperation.OpenConfiguration)
        {
            state = OpenConfiguration(state);
        }

        return new ApplicationInputResult(state, catalog);
    }

    public ApplicationState OpenConfiguration(ApplicationState state)
    {
        if (externalEditorLauncher is null)
        {
            return state with
            {
                Command = state.Command with { Error = "External editing is unavailable." }
            };
        }

        ExternalEditorResult result;
        terminalUi.SuspendForExternalProcess();
        try
        {
            result = externalEditorLauncher.Open(GlobalConfigurationPath.Resolve(), 1);
        }
        finally
        {
            terminalUi.ResumeAfterExternalProcess();
        }

        return result.Error is null
            ? state
            : state with { Command = state.Command with { Error = result.Error } };
    }
}
