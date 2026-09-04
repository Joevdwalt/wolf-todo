using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class TuiApplication(
    IApplicationConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader,
    ITerminalUi terminalUi,
    IApplicationStateStore applicationStateStore,
    ApplicationInputRouter inputRouter,
    TabHostPresenter tabPresenter,
    TabHostReducer tabReducer,
    ProjectBrowserPresenter browserPresenter,
    BrowserReducer browserReducer,
    string logo,
    DayPlannerPresenter? plannerPresenter = null,
    DayPlannerReducer? plannerReducer = null,
    ProjectTodoMutationService? mutationService = null,
    ApplicationCommandReducer? commandReducer = null,
    CommandPaletteReducer? paletteReducer = null,
    CommandPalettePresenter? palettePresenter = null,
    ApplicationActionCatalog? actionCatalog = null,
    IExternalEditorLauncher? externalEditorLauncher = null,
    PlannerCalendarAgendaCache? plannerCalendarCache = null,
    Func<DateOnly>? todayProvider = null,
    DayScheduleExportService? dayScheduleExportService = null,
    WeeklyTimeLogService? weeklyTimeLogService = null,
    Func<DateTime>? nowProvider = null,
    IPomodoroCompletionNotifier? pomodoroCompletionNotifier = null,
    PlannerWorkflow? plannerWorkflow = null,
    BrowserWorkflow? browserWorkflow = null,
    TimerWorkflow? timerWorkflow = null)
{
    private static readonly TabId TodosTab = new("todos");
    private static readonly TabId PlannerTab = new("planner");
    private static readonly ImmutableArray<TabDefinition> Tabs =
    [
        new(TodosTab, "Todos"),
        new(PlannerTab, "Day Planner")
    ];

    private readonly ApplicationCommandReducer commandReducer = commandReducer ?? new ApplicationCommandReducer();
    private readonly CommandPaletteReducer paletteReducer = paletteReducer ?? new CommandPaletteReducer();
    private readonly CommandPalettePresenter palettePresenter = palettePresenter ?? new CommandPalettePresenter();
    private readonly Func<DateOnly> todayProvider = todayProvider ??
        (() => DateOnly.FromDateTime(DateTime.Today));
    private readonly ApplicationActionCatalog actionCatalog = actionCatalog ??
        new ApplicationActionCatalog(todayProvider);
    private readonly PlannerWorkflow plannerWorkflow = plannerWorkflow ?? new PlannerWorkflow(
        plannerPresenter ?? new DayPlannerPresenter(),
        plannerReducer ?? new DayPlannerReducer(),
        plannerCalendarCache ?? new PlannerCalendarAgendaCache(new DisabledPlannerCalendarAgendaProvider()),
        dayScheduleExportService,
        externalEditorLauncher,
        terminalUi,
        catalogLoader);
    private readonly BrowserWorkflow browserWorkflow = browserWorkflow ?? new BrowserWorkflow(
        catalogLoader,
        externalEditorLauncher,
        terminalUi,
        todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today)));
    private readonly TimerWorkflow timerWorkflow = timerWorkflow ?? new TimerWorkflow(
        weeklyTimeLogService,
        nowProvider ?? (() => DateTime.Now),
        pomodoroCompletionNotifier,
        terminalUi);

    public int Run()
    {
        ApplicationConfiguration configuration;

        try
        {
            configuration = configurationLoader.Load();
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            terminalUi.ShowStartupError(exception.Message);
            return 1;
        }

        var catalog = catalogLoader.Load(configuration.ProjectFiles);
        var session = applicationStateStore.Load();
        var selectedProjectPath = session.SelectedProjectPath;
        var initialProjectIndex = FindProjectIndex(catalog, selectedProjectPath, configuration.SidebarItems.Length);
        var browserState = BrowserState.Initial with
        {
            ProjectIndex = initialProjectIndex,
            Focus = BrowserFocus.Todos,
            Sort = session.Sort
        };
        var state = new ApplicationState(TabHostState.CreateInitial(Tabs), browserState)
        {
            Planner = PlannerState.CreateInitial(todayProvider())
        };
        terminalUi.SetCursorVisible(false);

        try
        {
            terminalUi.ShowSplash(logo, configuration.Theme);
            terminalUi.ReadKey();

            while (true)
            {
                EnsureSupportedTab(state.Tabs.ActiveTab);
                state = timerWorkflow.CompletePomodoro(state, configuration, state.Tabs.ActiveTab == TodosTab);
                var tabView = tabPresenter.CreateView(Tabs, state.Tabs);
                BrowserView? browserView = null;
                PlannerView? plannerView = null;
                CommandPaletteView? paletteView = null;
                if (state.Tabs.ActiveTab == TodosTab)
                {
                    browserView = browserPresenter.CreateView(catalog, state.Browser, configuration.SidebarItems);
                    state = state with { Browser = browserView.State };
                    selectedProjectPath = browserView.SelectedProjectPath;
                    if (state.Palette.IsOpen)
                    {
                        paletteView = palettePresenter.CreateView(
                            state.Palette,
                            actionCatalog.Create(true, browserView, null, configuration.KeyBindings,
                                configuration.Planner.Export is not null,
                                configuration.Timer is not null,
                                state.Timer is not null));
                    }
                    var renderedBrowserView = browserView with
                    {
                        GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                        GlobalError = state.Command.Error,
                        CommandPalette = paletteView,
                        TimerStatus = timerWorkflow.Status(state.Timer),
                        TimerIsBright = timerWorkflow.IsBright(state.Timer),
                        PomodoroPrompt = state.PomodoroPrompt,
                        PomodoroCompletion = state.PomodoroCompletion
                    };
                    terminalUi.ShowBrowser(
                        tabView,
                        renderedBrowserView,
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
                        paletteView = palettePresenter.CreateView(
                            state.Palette,
                            actionCatalog.Create(false, null, plannerView, configuration.KeyBindings,
                                configuration.Planner.Export is not null,
                                configuration.Timer is not null,
                                state.Timer is not null));
                    }
                    var renderedPlannerView = plannerView with
                    {
                        GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                        GlobalError = state.Command.Error,
                        CommandPalette = paletteView,
                        TimerStatus = timerWorkflow.Status(state.Timer),
                        TimerIsBright = timerWorkflow.IsBright(state.Timer),
                        PomodoroPrompt = state.PomodoroPrompt,
                        PomodoroCompletion = state.PomodoroCompletion
                    };
                    terminalUi.ShowPlanner(
                        tabView,
                        renderedPlannerView,
                        configuration.KeyBindings,
                        configuration.Theme);
                }

                var pendingKey = state.Timer is not null
                    ? terminalUi.ReadKey(TimeSpan.FromSeconds(1))
                    : state.Tabs.ActiveTab == PlannerTab
                        ? terminalUi.ReadKey(plannerWorkflow.IsRefreshing
                            ? TimeSpan.FromMilliseconds(250)
                            : TimeSpan.FromMinutes(1))
                        : terminalUi.ReadKey();
                if (pendingKey is null)
                {
                    continue;
                }

                var key = pendingKey.Value;
                if (state.PomodoroCompletion is not null)
                {
                    state = state with { PomodoroCompletion = null };
                }
                if (state.PomodoroPrompt is not null)
                {
                    state = timerWorkflow.ReducePrompt(state, key, configuration, state.Tabs.ActiveTab == TodosTab);
                    continue;
                }

                var featureCapturesInput = state.Tabs.ActiveTab == TodosTab
                    ? state.Browser.IsFilterMode || state.Browser.IsSortMode ||
                      state.Browser.Editor is not null || state.Browser.BulkEditor is not null
                    : state.Planner.CapturesInput;

                if (state.Command.IsActive ||
                    (!featureCapturesInput && configuration.KeyBindings.MatchesCommandMode(key)))
                {
                    var commandTransition = commandReducer.Reduce(
                        state.Command,
                        key,
                        configuration.KeyBindings);
                    state = state with { Command = commandTransition.State };
                    if (commandTransition.Operation == ApplicationCommandOperation.Exit)
                    {
                        state = timerWorkflow.Stop(state, configuration, state.Tabs.ActiveTab == TodosTab);
                        if (state.Timer is null) return 0;
                        continue;
                    }

                    if (commandTransition.Operation == ApplicationCommandOperation.ToggleCompleted)
                    {
                        state = state with
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

                    if (commandTransition.Operation == ApplicationCommandOperation.OpenPalette)
                    {
                        state = state with
                        {
                            Palette = CommandPaletteState.Closed with { IsOpen = true }
                        };
                    }

                    if (commandTransition.Operation == ApplicationCommandOperation.MoveTodoProject)
                    {
                        var projectMoveResult = browserWorkflow.MoveSelectedTodoToProject(
                            state,
                            browserView,
                            commandTransition.ProjectTitle,
                            catalog,
                            configuration,
                            mutationService,
                            state.Tabs.ActiveTab == TodosTab);
                        state = projectMoveResult.State;
                        catalog = projectMoveResult.Catalog;
                    }

                    if (commandTransition.Operation == ApplicationCommandOperation.ArchiveCompleted)
                    {
                        var archiveResult = browserWorkflow.ArchiveCompletedProject(
                            state,
                            browserView,
                            catalog,
                            configuration,
                            mutationService,
                            state.Tabs.ActiveTab == TodosTab);
                        state = archiveResult.State;
                        catalog = archiveResult.Catalog;
                    }

                    if (commandTransition.Operation == ApplicationCommandOperation.RollProjectToday)
                    {
                        if (state.Tabs.ActiveTab != TodosTab || browserView is null)
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
                            var transition = browserReducer.ReduceAction(
                                state.Browser,
                                BrowserAction.RollProjectToday,
                                browserView);
                            var rollResult = browserWorkflow.ApplyTransition(
                                state,
                                transition,
                                catalog,
                                configuration,
                                mutationService);
                            state = rollResult.State;
                            catalog = rollResult.Catalog;
                        }
                    }

                    if (commandTransition.Operation == ApplicationCommandOperation.StartPomodoro)
                    {
                        state = timerWorkflow.StartPomodoroCommand(
                            state,
                            browserView,
                            plannerView,
                            catalog,
                            configuration,
                            commandTransition,
                            state.Tabs.ActiveTab == TodosTab);
                    }

                    if (commandTransition.Operation == ApplicationCommandOperation.DumpScreen)
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

                    continue;
                }

                if (state.Palette.IsOpen ||
                    (!featureCapturesInput && configuration.KeyBindings.MatchesCommandPalette(key)))
                {
                    paletteView ??= palettePresenter.CreateView(
                        state.Palette,
                        actionCatalog.Create(
                            state.Tabs.ActiveTab == TodosTab,
                            browserView,
                            plannerView,
                            configuration.KeyBindings,
                            configuration.Planner.Export is not null,
                            configuration.Timer is not null,
                            state.Timer is not null));
                    var paletteTransition = paletteReducer.Reduce(
                        state.Palette,
                        key,
                        configuration.KeyBindings,
                        paletteView);
                    state = state with { Palette = paletteTransition.State };
                    if (paletteTransition.Action is null)
                    {
                        continue;
                    }

                    var action = paletteTransition.Action.Value;
                    if (action == ApplicationActionId.Exit)
                    {
                        state = timerWorkflow.Stop(state, configuration, state.Tabs.ActiveTab == TodosTab);
                        if (state.Timer is null) return 0;
                        continue;
                    }

                    if (action == ApplicationActionId.ToggleTimer)
                    {
                        state = timerWorkflow.Toggle(state, browserView, plannerView, catalog, configuration, state.Tabs.ActiveTab == TodosTab);
                        continue;
                    }

                    if (action is ApplicationActionId.StartPomodoro or ApplicationActionId.StartUntrackedPomodoro)
                    {
                        state = timerWorkflow.OpenPomodoroPrompt(
                            state,
                            browserView,
                            plannerView,
                            catalog,
                            configuration,
                            action == ApplicationActionId.StartUntrackedPomodoro,
                            state.Tabs.ActiveTab == TodosTab);
                        continue;
                    }

                    if (action == ApplicationActionId.ToggleCompleted)
                    {
                        state = state with
                        {
                            Browser = state.Browser with
                            {
                                ShowCompleted = !state.Browser.ShowCompleted,
                                TodoIndex = 0,
                                PendingTodoSelection = null,
                                Error = null
                            }
                        };
                        continue;
                    }

                    if (action is ApplicationActionId.NextTab or ApplicationActionId.PreviousTab)
                    {
                        var direction = action == ApplicationActionId.NextTab
                            ? TabDirection.Next
                            : TabDirection.Previous;
                        var tabs = tabReducer.Move(state.Tabs, Tabs, direction);
                        state = state with
                        {
                            Tabs = tabs,
                            Browser = tabs.ActiveTab == TodosTab
                                ? state.Browser
                                : ClearBrowserMarks(state.Browser)
                        };
                        continue;
                    }

                    if (state.Tabs.ActiveTab == TodosTab)
                    {
                        var browserAction = action switch
                        {
                            ApplicationActionId.BrowserFilter => BrowserAction.Filter,
                            ApplicationActionId.BrowserSort => BrowserAction.Sort,
                            ApplicationActionId.BrowserCreate => BrowserAction.Create,
                            ApplicationActionId.BrowserEdit => BrowserAction.Edit,
                            ApplicationActionId.BrowserEditExternal => BrowserAction.EditExternal,
                            ApplicationActionId.BrowserToggleCompleted => BrowserAction.ToggleCompleted,
                            ApplicationActionId.BrowserToggleSelection => BrowserAction.ToggleSelection,
                            ApplicationActionId.BrowserBulkEdit => BrowserAction.BulkEdit,
                            ApplicationActionId.BrowserClearSelection => BrowserAction.ClearSelection,
                            ApplicationActionId.BrowserRollProjectToday => BrowserAction.RollProjectToday,
                            ApplicationActionId.BrowserToggleDetails => BrowserAction.ToggleDetails,
                            ApplicationActionId.BrowserJumpTop => BrowserAction.JumpTop,
                            ApplicationActionId.BrowserJumpBottom => BrowserAction.JumpBottom,
                            _ => (BrowserAction?)null
                        };
                        if (browserAction is not null)
                        {
                            var transition = browserReducer.ReduceAction(
                                state.Browser,
                                browserAction.Value,
                                browserView!);
                            var paletteBrowserResult = browserWorkflow.ApplyTransition(
                                state,
                                transition,
                                catalog,
                                configuration,
                                mutationService);
                            state = paletteBrowserResult.State;
                            catalog = paletteBrowserResult.Catalog;
                        }

                        continue;
                    }

                    if (action == ApplicationActionId.PlannerRefreshCalendar)
                    {
                        plannerWorkflow.Refresh(configuration, state.Planner);
                        continue;
                    }

                    if (action == ApplicationActionId.PlannerExportSchedule)
                    {
                        state = plannerWorkflow.Export(state, plannerView!, configuration);
                        continue;
                    }

                    var plannerAction = action switch
                    {
                        ApplicationActionId.PlannerPreviousDay => PlannerAction.PreviousDay,
                        ApplicationActionId.PlannerNextDay => PlannerAction.NextDay,
                        ApplicationActionId.PlannerToday => PlannerAction.Today,
                        ApplicationActionId.PlannerAssignOrMove => PlannerAction.AssignOrMove,
                        ApplicationActionId.PlannerUnschedule => PlannerAction.Unschedule,
                        ApplicationActionId.PlannerCreate => PlannerAction.Create,
                        ApplicationActionId.PlannerEdit => PlannerAction.Edit,
                        ApplicationActionId.PlannerEditExternal => PlannerAction.EditExternal,
                        ApplicationActionId.PlannerToggleCompleted => PlannerAction.ToggleCompleted,
                        ApplicationActionId.PlannerToggleDetails => PlannerAction.ToggleDetails,
                        _ => (PlannerAction?)null
                    };
                    if (plannerAction is not null)
                    {
                        var transition = plannerWorkflow.ReduceAction(
                            state.Planner,
                            plannerAction.Value,
                            configuration,
                            plannerView!);
                        var plannerResult = plannerWorkflow.ApplyTransition(
                            state,
                            transition,
                            catalog,
                            configuration,
                            mutationService);
                        state = plannerResult.State;
                        catalog = plannerResult.Catalog;
                    }

                    continue;
                }

                if (state.Command.Error is not null)
                {
                    state = state with { Command = state.Command with { Error = null } };
                }

                if (!featureCapturesInput && configuration.KeyBindings.MatchesToggleTimer(key))
                {
                    state = timerWorkflow.Toggle(state, browserView, plannerView, catalog, configuration, state.Tabs.ActiveTab == TodosTab);
                    continue;
                }

                if (!featureCapturesInput &&
                    (configuration.KeyBindings.MatchesStartPomodoro(key) ||
                     configuration.KeyBindings.MatchesStartUntrackedPomodoro(key)))
                {
                    state = timerWorkflow.OpenPomodoroPrompt(
                        state,
                        browserView,
                        plannerView,
                        catalog,
                        configuration,
                        configuration.KeyBindings.MatchesStartUntrackedPomodoro(key),
                        state.Tabs.ActiveTab == TodosTab);
                    continue;
                }

                var inputRoute = inputRouter.Route(
                    featureCapturesInput,
                    key,
                    configuration.KeyBindings);

                if (inputRoute is ApplicationInputRoute.NextTab or ApplicationInputRoute.PreviousTab)
                {
                    var direction = inputRoute == ApplicationInputRoute.PreviousTab
                        ? TabDirection.Previous
                        : TabDirection.Next;
                    var tabs = tabReducer.Move(state.Tabs, Tabs, direction);
                    state = state with
                    {
                        Tabs = tabs,
                        Browser = tabs.ActiveTab == TodosTab
                            ? state.Browser
                            : ClearBrowserMarks(state.Browser)
                    };
                    continue;
                }

                if (state.Tabs.ActiveTab == PlannerTab)
                {
                    if (!state.Planner.CapturesInput &&
                        configuration.KeyBindings.MatchesPlannerRefreshCalendar(key))
                    {
                        plannerWorkflow.Refresh(configuration, state.Planner);
                        continue;
                    }

                    if (!state.Planner.CapturesInput &&
                        configuration.KeyBindings.MatchesPlannerExportSchedule(key))
                    {
                        state = plannerWorkflow.Export(state, plannerView!, configuration);
                        continue;
                    }

                    var transition = plannerWorkflow.Reduce(
                        state.Planner,
                        key,
                        configuration,
                        plannerView!);
                    var plannerResult = plannerWorkflow.ApplyTransition(
                        state,
                        transition,
                        catalog,
                        configuration,
                        mutationService);
                    state = plannerResult.State;
                    catalog = plannerResult.Catalog;

                    continue;
                }

                var browserTransition = browserReducer.Reduce(state.Browser, key, configuration, browserView!);
                var browserResult = browserWorkflow.ApplyTransition(
                    state,
                    browserTransition,
                    catalog,
                    configuration,
                    mutationService);
                state = browserResult.State;
                catalog = browserResult.Catalog;
            }
        }
        finally
        {
            applicationStateStore.Save(new ApplicationSessionState(
                selectedProjectPath,
                state.Browser.Sort));
            terminalUi.SetCursorVisible(true);
        }
    }

    private static BrowserState ClearBrowserMarks(BrowserState state) => state with
    {
        MarkedTodos = [],
        BulkEditor = null,
        StatusMessage = null
    };

    private static int FindProjectIndex(
        ProjectCatalog catalog,
        string? selectedProjectPath,
        int savedSidebarItemCount)
    {
        if (selectedProjectPath is null)
        {
            return 0;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        for (var index = 0; index < catalog.Projects.Length; index++)
        {
            if (string.Equals(catalog.Projects[index].Path, selectedProjectPath, comparison))
            {
                return index + savedSidebarItemCount + 2;
            }
        }

        for (var index = 0; index < catalog.Errors.Length; index++)
        {
            if (string.Equals(catalog.Errors[index].Path, selectedProjectPath, comparison))
            {
                return catalog.Projects.Length + savedSidebarItemCount + index + 2;
            }
        }

        return 0;
    }

    private static void EnsureSupportedTab(TabId activeTab)
    {
        if (activeTab != TodosTab && activeTab != PlannerTab)
        {
            throw new InvalidOperationException($"No feature is registered for tab '{activeTab.Value}'.");
        }
    }
}
