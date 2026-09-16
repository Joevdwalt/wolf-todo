using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class TuiApplication
{
    private static readonly TabId TodosTab = new("todos");
    private static readonly TabId PlannerTab = new("planner");
    private static readonly ImmutableArray<TabDefinition> Tabs =
    [
        new(TodosTab, "Todos"),
        new(PlannerTab, "Day Planner")
    ];

    private readonly IApplicationConfigurationLoader configurationLoader;
    private readonly ProjectCatalogLoader catalogLoader;
    private readonly ITerminalUi terminalUi;
    private readonly IApplicationStateStore applicationStateStore;
    private readonly TabHostPresenter tabPresenter;
    private readonly TabHostReducer tabReducer;
    private readonly ProjectBrowserPresenter browserPresenter;
    private readonly BrowserReducer browserReducer;
    private readonly string logo;
    private readonly ProjectTodoMutationService? mutationService;
    private readonly IExternalEditorLauncher? externalEditorLauncher;
    private readonly PlannerCalendarAgendaCache? plannerCalendarCache;
    private readonly Func<DateOnly> todayProvider;
    private readonly IApplicationFileChangeMonitor? fileChangeMonitor;
    private readonly ApplicationLoopWaiter? applicationLoopWaiter;
    private readonly RuntimeReloadCoordinator? runtimeReloadCoordinator;
    private readonly string? startupTaskCode;
    private readonly ApplicationActionCatalog actionCatalog;
    private readonly PlannerWorkflow plannerWorkflow;
    private readonly BrowserWorkflow browserWorkflow;
    private readonly TimerWorkflow timerWorkflow;
    private readonly FocusedTaskPresenter focusedTaskPresenter;
    private readonly FocusedTaskReducer focusedTaskReducer;
    private readonly FocusedTaskWorkflow focusedTaskWorkflow;
    private readonly TaskLinkWorkflow taskLinkWorkflow;
    private readonly ApplicationCommandDispatcher commandDispatcher;
    private readonly ApplicationPaletteDispatcher paletteDispatcher;
    private readonly ApplicationFrameCoordinator frameCoordinator;
    private readonly ApplicationInputDispatcher inputDispatcher;

    public TuiApplication(
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
        TimerWorkflow? timerWorkflow = null,
        IApplicationFileChangeMonitor? fileChangeMonitor = null,
        ApplicationLoopWaiter? applicationLoopWaiter = null,
        RuntimeReloadCoordinator? runtimeReloadCoordinator = null,
        string? startupTaskCode = null,
        ApplicationFrameCoordinator? frameCoordinator = null,
        ApplicationCommandDispatcher? commandDispatcher = null,
        ApplicationPaletteDispatcher? paletteDispatcher = null,
        ApplicationInputDispatcher? inputDispatcher = null)
    {
        this.configurationLoader = configurationLoader;
        this.catalogLoader = catalogLoader;
        this.terminalUi = terminalUi;
        this.applicationStateStore = applicationStateStore;
        this.tabPresenter = tabPresenter;
        this.tabReducer = tabReducer;
        this.browserPresenter = browserPresenter;
        this.browserReducer = browserReducer;
        this.logo = logo;
        this.mutationService = mutationService;
        this.externalEditorLauncher = externalEditorLauncher;
        this.plannerCalendarCache = plannerCalendarCache;
        this.todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
        this.fileChangeMonitor = fileChangeMonitor;
        this.applicationLoopWaiter = applicationLoopWaiter;
        this.runtimeReloadCoordinator = runtimeReloadCoordinator;
        this.startupTaskCode = startupTaskCode;
        this.actionCatalog = actionCatalog ?? new ApplicationActionCatalog(this.todayProvider);
        this.plannerWorkflow = plannerWorkflow ?? new PlannerWorkflow(
            plannerPresenter ?? new DayPlannerPresenter(),
            plannerReducer ?? new DayPlannerReducer(),
            plannerCalendarCache ?? new PlannerCalendarAgendaCache(new DisabledPlannerCalendarAgendaProvider()),
            dayScheduleExportService,
            externalEditorLauncher,
            terminalUi,
            catalogLoader);
        this.browserWorkflow = browserWorkflow ?? new BrowserWorkflow(
            catalogLoader,
            externalEditorLauncher,
            terminalUi,
            this.todayProvider);
        this.timerWorkflow = timerWorkflow ?? new TimerWorkflow(
            weeklyTimeLogService,
            nowProvider ?? (() => DateTime.Now),
            pomodoroCompletionNotifier,
            terminalUi);
        this.focusedTaskPresenter = new FocusedTaskPresenter();
        this.focusedTaskReducer = new FocusedTaskReducer(this.todayProvider);
        this.focusedTaskWorkflow = new FocusedTaskWorkflow(
            catalogLoader,
            terminalUi,
            externalEditorLauncher);
        this.taskLinkWorkflow = new TaskLinkWorkflow();
        var resolvedPalettePresenter = palettePresenter ?? new CommandPalettePresenter();
        this.commandDispatcher = commandDispatcher ?? new ApplicationCommandDispatcher(
            commandReducer ?? new ApplicationCommandReducer(),
            this.timerWorkflow,
            this.browserWorkflow,
            this.focusedTaskWorkflow,
            this.taskLinkWorkflow,
            browserReducer,
            terminalUi,
            externalEditorLauncher,
            mutationService);
        this.paletteDispatcher = paletteDispatcher ?? new ApplicationPaletteDispatcher(
            paletteReducer ?? new CommandPaletteReducer(),
            resolvedPalettePresenter,
            this.actionCatalog,
            this.commandDispatcher,
            this.timerWorkflow,
            this.taskLinkWorkflow,
            this.focusedTaskReducer,
            this.focusedTaskWorkflow,
            browserReducer,
            this.browserWorkflow,
            this.plannerWorkflow,
            tabReducer,
            mutationService);
        this.frameCoordinator = frameCoordinator ?? new ApplicationFrameCoordinator(
            terminalUi,
            tabPresenter,
            browserPresenter,
            this.plannerWorkflow,
            this.focusedTaskPresenter,
            resolvedPalettePresenter,
            this.actionCatalog,
            this.timerWorkflow);
        this.inputDispatcher = inputDispatcher ?? new ApplicationInputDispatcher(
            inputRouter,
            this.commandDispatcher,
            this.paletteDispatcher,
            this.timerWorkflow,
            this.taskLinkWorkflow,
            this.focusedTaskReducer,
            this.focusedTaskWorkflow,
            tabReducer,
            this.plannerWorkflow,
            this.browserWorkflow,
            browserReducer,
            mutationService);
    }

    public int Run()
    {
        var runtimeMonitor = fileChangeMonitor ?? NullApplicationFileChangeMonitor.Instance;
        var loopWaiter = applicationLoopWaiter ?? new ApplicationLoopWaiter(terminalUi, runtimeMonitor);
        var reloadCoordinator = runtimeReloadCoordinator ?? new RuntimeReloadCoordinator(
            configurationLoader,
            catalogLoader,
            runtimeMonitor,
            plannerCalendarCache ?? new PlannerCalendarAgendaCache(new DisabledPlannerCalendarAgendaProvider()));
        ApplicationConfiguration configuration;

        try
        {
            configuration = configurationLoader.Load();
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            terminalUi.ShowStartupError(exception.Message);
            runtimeMonitor.Dispose();
            return 1;
        }

        var catalog = catalogLoader.Load(configuration.ProjectFiles);
        runtimeMonitor.WatchProjectFiles(configuration.ProjectFiles);
        plannerCalendarCache?.EnsureWindow(configuration.GoogleCalendar);
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
        if (startupTaskCode is not null)
            state = taskLinkWorkflow.Open(state, catalog, startupTaskCode, configuration.SidebarItems.Length);
        var selectionAnchor = new SidebarSelectionAnchor(
            initialProjectIndex == 0 ? ProjectRowKind.All : ProjectRowKind.Project,
            selectedProjectPath);
        terminalUi.SetCursorVisible(false);

        try
        {
            terminalUi.ShowSplashAndWaitForDismissal(logo, configuration.Theme);

            while (true)
            {
                EnsureSupportedTab(state.Tabs.ActiveTab);
                state = timerWorkflow.CompletePomodoro(state, configuration, state.Tabs.ActiveTab == TodosTab);
                var frame = frameCoordinator.Render(
                    Tabs,
                    state,
                    catalog,
                    configuration,
                    selectionAnchor,
                    selectedProjectPath);
                state = frame.State;
                catalog = frame.Catalog;
                selectionAnchor = frame.SelectionAnchor;
                selectedProjectPath = frame.SelectedProjectPath;
                if (frame.Retry) continue;

                var redrawInterval = state.Timer is not null
                    ? TimeSpan.FromSeconds(1)
                    : state.Tabs.ActiveTab == PlannerTab
                        ? plannerWorkflow.IsRefreshing
                            ? TimeSpan.FromMilliseconds(250)
                            : TimeSpan.FromMinutes(1)
                        : TimeSpan.FromMinutes(1);
                var loopEvent = loopWaiter.Wait(redrawInterval);
                if (loopEvent.FileChanges.HasChanges)
                {
                    var reload = reloadCoordinator.Reload(
                        state,
                        configuration,
                        catalog,
                        selectionAnchor,
                        loopEvent.FileChanges);
                    state = reload.State;
                    configuration = reload.Configuration;
                    catalog = reload.Catalog;
                    continue;
                }

                if (loopEvent.RedrawRequested) continue;

                var key = loopEvent.Key!.Value;
                var input = inputDispatcher.Handle(
                    new ApplicationInputContext(
                        state,
                        catalog,
                        configuration,
                        Tabs,
                        frame.BrowserView,
                        frame.PlannerView,
                        frame.FocusedTaskView,
                        frame.PaletteView),
                    key);
                state = input.State;
                catalog = input.Catalog;
                if (input.Exit) return 0;
                continue;
            }
        }
        finally
        {
            applicationStateStore.Save(new ApplicationSessionState(
                selectedProjectPath,
                state.Browser.Sort));
            runtimeMonitor.Dispose();
            terminalUi.SetCursorVisible(true);
        }
    }

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
