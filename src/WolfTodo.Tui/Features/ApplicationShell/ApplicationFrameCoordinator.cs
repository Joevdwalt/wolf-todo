using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationFrameCoordinator(
    ITerminalUi terminalUi,
    TabHostPresenter tabPresenter,
    ProjectBrowserPresenter browserPresenter,
    PlannerWorkflow plannerWorkflow,
    FocusedTaskPresenter focusedTaskPresenter,
    CommandPalettePresenter palettePresenter,
    ApplicationActionCatalog actionCatalog,
    TimerWorkflow timerWorkflow)
{
    public ApplicationFrameResult Render(
        ImmutableArray<TabDefinition> tabs,
        ApplicationState state,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        SidebarSelectionAnchor selectionAnchor,
        string? selectedProjectPath)
    {
        var tabView = tabPresenter.CreateView(tabs, state.Tabs);
        BrowserView? browserView = null;
        PlannerView? plannerView = null;
        FocusedTaskView? focusedTaskView = null;
        CommandPaletteView? paletteView = null;
        if (state.TaskLinkPanel is not null)
        {
            terminalUi.ShowTaskLinkPanel(state.TaskLinkPanel, configuration.Theme);
            return new ApplicationFrameResult(
                state,
                catalog,
                null,
                null,
                null,
                null,
                selectionAnchor,
                selectedProjectPath,
                false);
        }

        if (state.FocusedTask is not null)
        {
            focusedTaskView = focusedTaskPresenter.CreateView(catalog, state.FocusedTask);
            if (focusedTaskView is null)
            {
                return ApplicationFrameResult.RetryFrame(
                    FocusedTaskWorkflow.CloseWithMessage(
                        state,
                        "Focused task is no longer available."),
                    catalog,
                    selectionAnchor,
                    selectedProjectPath);
            }

            state = state with { FocusedTask = focusedTaskView.State };
            if (state.Palette.IsOpen)
            {
                paletteView = CreatePalette(state, configuration, focusedTaskView, null, null);
            }

            terminalUi.ShowFocusedTask(
                focusedTaskView with
                {
                    GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                    GlobalError = state.Command.Error,
                    CommandPalette = paletteView,
                    TimerStatus = timerWorkflow.Status(state.Timer),
                    TimerIsBright = timerWorkflow.IsBright(state.Timer),
                    PomodoroPrompt = state.PomodoroPrompt,
                    PomodoroCompletion = state.PomodoroCompletion,
                    ReloadStatus = state.ReloadStatus
                },
                configuration.KeyBindings,
                configuration.Theme);
        }
        else if (state.Tabs.ActiveTab.Value == "todos")
        {
            browserView = browserPresenter.CreateView(catalog, state.Browser, configuration.SidebarItems);
            state = state with { Browser = browserView.State };
            selectionAnchor = SidebarSelectionAnchor.Capture(browserView);
            selectedProjectPath = browserView.SelectedProjectPath;
            if (state.Palette.IsOpen)
            {
                paletteView = CreatePalette(state, configuration, null, browserView, null);
            }

            terminalUi.ShowBrowser(
                tabView,
                browserView with
                {
                    GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                    GlobalError = state.Command.Error,
                    CommandPalette = paletteView,
                    TimerStatus = timerWorkflow.Status(state.Timer),
                    TimerIsBright = timerWorkflow.IsBright(state.Timer),
                    PomodoroPrompt = state.PomodoroPrompt,
                    PomodoroCompletion = state.PomodoroCompletion,
                    ReloadStatus = state.ReloadStatus
                },
                configuration.KeyBindings,
                configuration.Theme);
        }
        else
        {
            plannerView = plannerWorkflow.CreateView(
                catalog,
                state.Planner,
                configuration,
                timerWorkflow.ActiveFocusBlock(state.Timer));
            state = state with { Planner = plannerView.State };
            if (state.Palette.IsOpen)
            {
                paletteView = CreatePalette(state, configuration, null, null, plannerView);
            }

            terminalUi.ShowPlanner(
                tabView,
                plannerView with
                {
                    GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                    GlobalError = state.Command.Error,
                    CommandPalette = paletteView,
                    TimerStatus = timerWorkflow.Status(state.Timer),
                    TimerIsBright = timerWorkflow.IsBright(state.Timer),
                    PomodoroPrompt = state.PomodoroPrompt,
                    PomodoroCompletion = state.PomodoroCompletion,
                    ReloadStatus = state.ReloadStatus
                },
                configuration.KeyBindings,
                configuration.Theme);
        }

        return new ApplicationFrameResult(
            state,
            catalog,
            browserView,
            plannerView,
            focusedTaskView,
            paletteView,
            selectionAnchor,
            selectedProjectPath,
            false);
    }

    private CommandPaletteView CreatePalette(
        ApplicationState state,
        ApplicationConfiguration configuration,
        FocusedTaskView? focusedTaskView,
        BrowserView? browserView,
        PlannerView? plannerView) =>
        palettePresenter.CreateView(
            state.Palette,
            actionCatalog.Create(
                state.Tabs.ActiveTab.Value == "todos",
                browserView,
                plannerView,
                configuration.KeyBindings,
                configuration.Planner.Export is not null,
                configuration.Timer is not null,
                state.Timer is not null,
                focusedTaskView));
}
