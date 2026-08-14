using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
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
    IPomodoroCompletionNotifier? pomodoroCompletionNotifier = null)
{
    private static readonly TabId TodosTab = new("todos");
    private static readonly TabId PlannerTab = new("planner");
    private static readonly ImmutableArray<TabDefinition> Tabs =
    [
        new(TodosTab, "Todos"),
        new(PlannerTab, "Day Planner")
    ];

    private readonly DayPlannerPresenter plannerPresenter = plannerPresenter ?? new DayPlannerPresenter();
    private readonly DayPlannerReducer plannerReducer = plannerReducer ?? new DayPlannerReducer();
    private readonly ApplicationCommandReducer commandReducer = commandReducer ?? new ApplicationCommandReducer();
    private readonly CommandPaletteReducer paletteReducer = paletteReducer ?? new CommandPaletteReducer();
    private readonly CommandPalettePresenter palettePresenter = palettePresenter ?? new CommandPalettePresenter();
    private readonly Func<DateOnly> todayProvider = todayProvider ??
        (() => DateOnly.FromDateTime(DateTime.Today));
    private readonly ApplicationActionCatalog actionCatalog = actionCatalog ??
        new ApplicationActionCatalog(todayProvider);
    private readonly IExternalEditorLauncher? externalEditorLauncher = externalEditorLauncher;
    private readonly PlannerCalendarAgendaCache plannerCalendarCache = plannerCalendarCache ??
        new PlannerCalendarAgendaCache(new DisabledPlannerCalendarAgendaProvider());
    private readonly DayScheduleExportService? dayScheduleExportService = dayScheduleExportService;
    private readonly WeeklyTimeLogService? weeklyTimeLogService = weeklyTimeLogService;
    private readonly Func<DateTime> nowProvider = nowProvider ?? (() => DateTime.Now);
    private readonly IPomodoroCompletionNotifier? pomodoroCompletionNotifier = pomodoroCompletionNotifier;

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
                state = CompletePomodoro(state, configuration);
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
                        TimerStatus = TimerStatus(state.Timer),
                        TimerIsBright = TimerIsBright(state.Timer),
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
                    var agenda = plannerCalendarCache.GetAgenda(
                        configuration.GoogleCalendar,
                        state.Planner.SelectedDate);
                    plannerView = plannerPresenter.CreateView(
                        catalog,
                        state.Planner,
                        agenda,
                        configuration.Planner,
                        ActiveFocusBlock(state.Timer));
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
                        TimerStatus = TimerStatus(state.Timer),
                        TimerIsBright = TimerIsBright(state.Timer),
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
                        ? terminalUi.ReadKey(plannerCalendarCache.IsRefreshing
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
                    state = ReducePomodoroPrompt(state, key, configuration);
                    continue;
                }

                var featureCapturesInput = state.Tabs.ActiveTab == TodosTab
                    ? state.Browser.IsFilterMode || state.Browser.IsSortMode ||
                      state.Browser.Editor is not null
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
                        state = StopTimer(state, configuration);
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
                        state = MoveSelectedTodoToProject(
                            state,
                            browserView,
                            commandTransition.ProjectTitle,
                            ref catalog,
                            configuration,
                            mutationService);
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
                            state = ApplyBrowserTransition(
                                state,
                                transition,
                                ref catalog,
                                configuration,
                                mutationService);
                        }
                    }

                    if (commandTransition.Operation == ApplicationCommandOperation.StartPomodoro)
                    {
                        state = StartPomodoroCommand(
                            state,
                            browserView,
                            plannerView,
                            catalog,
                            configuration,
                            commandTransition);
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
                        state = StopTimer(state, configuration);
                        if (state.Timer is null) return 0;
                        continue;
                    }

                    if (action == ApplicationActionId.ToggleTimer)
                    {
                        state = ToggleTimer(state, browserView, plannerView, catalog, configuration);
                        continue;
                    }

                    if (action is ApplicationActionId.StartPomodoro or ApplicationActionId.StartUntrackedPomodoro)
                    {
                        state = OpenPomodoroPrompt(
                            state,
                            browserView,
                            plannerView,
                            catalog,
                            configuration,
                            action == ApplicationActionId.StartUntrackedPomodoro);
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
                        state = state with { Tabs = tabReducer.Move(state.Tabs, Tabs, direction) };
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
                            state = ApplyBrowserTransition(
                                state,
                                transition,
                                ref catalog,
                                configuration,
                                mutationService);
                        }

                        continue;
                    }

                    if (action == ApplicationActionId.PlannerRefreshCalendar)
                    {
                        plannerCalendarCache.Refresh(
                            configuration.GoogleCalendar,
                            state.Planner.SelectedDate);
                        continue;
                    }

                    if (action == ApplicationActionId.PlannerExportSchedule)
                    {
                        state = ExportDaySchedule(state, plannerView!, configuration);
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
                        var transition = plannerReducer.ReduceAction(
                            state.Planner,
                            plannerAction.Value,
                            plannerView!,
                            configuration.Planner.DefaultDuration);
                        state = ApplyPlannerTransition(
                            state,
                            transition,
                            ref catalog,
                            configuration,
                            mutationService);
                    }

                    continue;
                }

                if (state.Command.Error is not null)
                {
                    state = state with { Command = state.Command with { Error = null } };
                }

                if (!featureCapturesInput && configuration.KeyBindings.MatchesToggleTimer(key))
                {
                    state = ToggleTimer(state, browserView, plannerView, catalog, configuration);
                    continue;
                }

                if (!featureCapturesInput &&
                    (configuration.KeyBindings.MatchesStartPomodoro(key) ||
                     configuration.KeyBindings.MatchesStartUntrackedPomodoro(key)))
                {
                    state = OpenPomodoroPrompt(
                        state,
                        browserView,
                        plannerView,
                        catalog,
                        configuration,
                        configuration.KeyBindings.MatchesStartUntrackedPomodoro(key));
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
                    state = state with { Tabs = tabReducer.Move(state.Tabs, Tabs, direction) };
                    continue;
                }

                if (state.Tabs.ActiveTab == PlannerTab)
                {
                    if (!state.Planner.CapturesInput &&
                        configuration.KeyBindings.MatchesPlannerRefreshCalendar(key))
                    {
                        plannerCalendarCache.Refresh(
                            configuration.GoogleCalendar,
                            state.Planner.SelectedDate);
                        continue;
                    }

                    if (!state.Planner.CapturesInput &&
                        configuration.KeyBindings.MatchesPlannerExportSchedule(key))
                    {
                        state = ExportDaySchedule(state, plannerView!, configuration);
                        continue;
                    }

                    var transition = plannerReducer.Reduce(
                        state.Planner,
                        key,
                        configuration.KeyBindings,
                        plannerView!,
                        configuration.Planner.DefaultDuration);
                    state = ApplyPlannerTransition(
                        state,
                        transition,
                        ref catalog,
                        configuration,
                        mutationService);

                    continue;
                }

                var browserTransition = browserReducer.Reduce(state.Browser, key, configuration, browserView!);
                state = ApplyBrowserTransition(
                    state,
                    browserTransition,
                    ref catalog,
                    configuration,
                    mutationService);
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

    private ApplicationState ExportDaySchedule(
        ApplicationState state,
        PlannerView view,
        ApplicationConfiguration configuration)
    {
        if (dayScheduleExportService is null)
        {
            return PlannerFailure(state, "Day schedule export is unavailable.");
        }

        var result = dayScheduleExportService.Export(view, configuration.Planner.Export);
        return result.Succeeded
            ? state with
            {
                Planner = state.Planner with { Error = $"Exported day schedule to {result.Path}" }
            }
            : PlannerFailure(state, result.Error ?? "Could not export day schedule.");
    }

    private ApplicationState ApplyPlannerTransition(
        ApplicationState state,
        PlannerTransition transition,
        ref ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? service)
    {
        state = state with { Planner = transition.State };
        if (transition.Operation == PlannerOperation.None)
        {
            return state;
        }

        if (transition.Operation == PlannerOperation.EditExternal)
        {
            if (externalEditorLauncher is null ||
                transition.ProjectPath is null ||
                transition.TodoIdentity is null)
            {
                return PlannerFailure(state, "External editing is unavailable.");
            }

            ExternalEditorResult externalResult;
            terminalUi.SuspendForExternalProcess();
            try
            {
                externalResult = externalEditorLauncher.Open(
                    transition.ProjectPath,
                    transition.TodoIdentity.SourceLine);
            }
            finally
            {
                terminalUi.ResumeAfterExternalProcess();
            }

            if (externalResult.Started)
            {
                catalog = catalogLoader.Load(configuration.ProjectFiles);
            }

            return externalResult.Error is null
                ? PlannerSuccess(state)
                : PlannerFailure(state, externalResult.Error);
        }

        if (service is null)
        {
            return PlannerFailure(state, "Todo writing is unavailable.");
        }

        var expected = FindTodo(catalog, transition.TodoIdentity);
        var latestCatalog = catalogLoader.Load(configuration.ProjectFiles);
        catalog = latestCatalog;
        var schedule = transition.ScheduleTarget == PlannerScheduleTarget.AllDay
            ? new TodoSchedule(state.Planner.SelectedDate)
            : new TodoSchedule(
                state.Planner.SelectedDate,
                new TimeOnly(6, 0).AddMinutes(state.Planner.SlotIndex * 15));

        if (transition.Operation == PlannerOperation.Create)
        {
            if (transition.ProjectPath is null || transition.Update is null)
            {
                return PlannerFailure(state, "The new todo is incomplete.");
            }

            if (transition.Update.Fields.Schedule is null)
            {
                return PlannerFailure(state, "A schedule is required when creating from Planner.");
            }

            var created = service.Create(transition.ProjectPath, transition.Update);
            if (!created.Succeeded)
            {
                return PlannerFailure(state, created.Error ?? "The todo could not be created.");
            }

            catalog = catalogLoader.Load(configuration.ProjectFiles);
            return PlannerSuccess(
                state,
                transition.Update.Fields.Schedule,
                created.SourceLine is { } createdLine
                    ? new TodoIdentity(transition.ProjectPath, createdLine)
                    : null);
        }

        if (transition.TodoIdentity is null)
        {
            return PlannerFailure(state, "The selected todo cannot be updated.");
        }

        if (expected is null)
        {
            return PlannerFailure(state, "The selected todo cannot be found.");
        }

        var result = transition.Operation switch
        {
            PlannerOperation.Schedule => service.SetSchedule(
                transition.TodoIdentity.ProjectPath,
                expected,
                schedule),
            PlannerOperation.Unschedule => service.SetSchedule(
                transition.TodoIdentity.ProjectPath,
                expected,
                null),
            PlannerOperation.Update when transition.Update is not null => service.UpdateTask(
                transition.TodoIdentity.ProjectPath,
                expected,
                transition.Update),
            PlannerOperation.ToggleCompleted => service.SetCompleted(
                transition.TodoIdentity.ProjectPath,
                expected,
                !expected.IsCompleted),
            _ => TodoMutationResult.Failure("The requested planner change is invalid.")
        };
        if (!result.Succeeded)
        {
            return PlannerFailure(state, result.Error ?? "The selected todo could not be updated.");
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var followedSchedule = transition.Operation switch
        {
            PlannerOperation.Schedule => schedule,
            PlannerOperation.Update => transition.Update?.Fields.Schedule,
            _ => null
        };
        return PlannerSuccess(
            state,
            followedSchedule,
            followedSchedule is null ? null : transition.TodoIdentity);
    }

    private static bool IsOccupied(
        ProjectCatalog catalog,
        TodoSchedule schedule,
        TimeSpan duration,
        TodoIdentity? excluded,
        TimeSpan defaultDuration)
    {
        if (schedule.Time is null)
        {
            return false;
        }

        if (duration > new TimeOnly(22, 0).ToTimeSpan() - schedule.Time.Value.ToTimeSpan())
        {
            return true;
        }

        var start = schedule.Time.Value;
        var end = start.Add(duration);
        return catalog.Projects
        .SelectMany(project => Flatten(project.Todos).Select(todo => (project.Path, Todo: todo)))
        .Any(candidate =>
            candidate.Todo.Schedule?.Date == schedule.Date &&
            candidate.Todo.Schedule.Time is not null &&
            candidate.Todo.Schedule.Time.Value < end &&
            candidate.Todo.Schedule.Time.Value.Add(candidate.Todo.Duration ?? defaultDuration) > start &&
            (excluded is null ||
             candidate.Path != excluded.ProjectPath ||
             candidate.Todo.SourceLine != excluded.SourceLine));
    }

    private static ApplicationState PlannerSuccess(
        ApplicationState state,
        TodoSchedule? follow = null,
        TodoIdentity? followIdentity = null) => state with
    {
        Planner = state.Planner with
        {
            SelectedDate = follow?.Date ?? state.Planner.SelectedDate,
            Focus = follow is null
                ? state.Planner.Focus
                : follow.Time is null ? PlannerFocus.AllDay : PlannerFocus.Timeline,
            SlotIndex = follow?.Time is { } time
                ? ((time.Hour - 6) * 4) + (time.Minute / 15)
                : state.Planner.SlotIndex,
            PendingAllDaySelection = follow?.Time is null ? followIdentity : null,
            Mode = PlannerMode.Browse,
            MovingTodo = null,
            Editor = null,
            Error = null
        }
    };

    private static ApplicationState PlannerFailure(ApplicationState state, string error) => state with
    {
        Planner = state.Planner with
        {
            Error = error,
            Editor = state.Planner.Editor is null ? null : state.Planner.Editor with { Error = error }
        }
    };

    private ApplicationState ApplyBrowserTransition(
        ApplicationState state,
        BrowserTransition transition,
        ref ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? service)
    {
        state = state with { Browser = transition.State };
        if (transition.Operation == BrowserOperation.None)
        {
            return state;
        }

        if (transition.Operation == BrowserOperation.EditExternal)
        {
            return ApplyExternalEdit(state, transition, ref catalog, configuration);
        }

        var expectedCatalog = catalog;
        var latestCatalog = catalogLoader.Load(configuration.ProjectFiles);
        catalog = latestCatalog;
        var result = ApplyBrowserOperation(
            transition,
            expectedCatalog,
            service,
            todayProvider());
        state = state with
        {
            Browser = state.Browser with
            {
                Error = result.Error,
                Editor = result.Succeeded
                    ? null
                    : state.Browser.Editor is null
                        ? null
                        : state.Browser.Editor with { Error = result.Error },
                PendingTodoSelection = result.Succeeded && result.SourceLine is not null &&
                                       transition.ProjectPath is not null
                    ? new TodoIdentity(transition.ProjectPath, result.SourceLine.Value)
                    : result.Succeeded && transition.Operation == BrowserOperation.RollProjectToday
                        ? transition.TodoIdentity
                        : null
            }
        };
        if (result.Succeeded)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        return state;
    }

    private ApplicationState ApplyExternalEdit(
        ApplicationState state,
        BrowserTransition transition,
        ref ProjectCatalog catalog,
        ApplicationConfiguration configuration)
    {
        if (externalEditorLauncher is null ||
            transition.ProjectPath is null ||
            transition.TodoIdentity is null)
        {
            return state with
            {
                Browser = state.Browser with { Error = "External editing is unavailable." }
            };
        }

        ExternalEditorResult result;
        terminalUi.SuspendForExternalProcess();
        try
        {
            result = externalEditorLauncher.Open(
                transition.ProjectPath,
                transition.TodoIdentity.SourceLine);
        }
        finally
        {
            terminalUi.ResumeAfterExternalProcess();
        }

        if (result.Started)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        return state with
        {
            Browser = state.Browser with
            {
                PendingTodoSelection = null,
                Error = result.Error
            }
        };
    }

    private ApplicationState MoveSelectedTodoToProject(
        ApplicationState state,
        BrowserView? view,
        string? targetTitle,
        ref ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? service)
    {
        if (state.Tabs.ActiveTab != TodosTab || view?.SelectedTodoIdentity is not { } identity)
        {
            return state with { Browser = state.Browser with { Error = "Select a todo in the Todos tab before moving it." } };
        }

        var target = catalog.Projects.FirstOrDefault(project =>
            string.Equals(project.Title, targetTitle, StringComparison.OrdinalIgnoreCase));
        var source = catalog.Projects.FirstOrDefault(project => project.Path == identity.ProjectPath);
        var todo = source is null ? null : Flatten(source.Todos).FirstOrDefault(item => item.SourceLine == identity.SourceLine);
        if (target is null)
        {
            return state with { Browser = state.Browser with { Error = $"Project not found: {targetTitle}" } };
        }
        if (todo is null || service is null)
        {
            return state with { Browser = state.Browser with { Error = "The selected todo cannot be moved." } };
        }

        var result = service.Move(source!.Path, target.Path, todo);
        if (!result.Succeeded)
        {
            return state with { Browser = state.Browser with { Error = result.Error } };
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var targetIndex = catalog.Projects
            .Select((project, index) => (project, index))
            .FirstOrDefault(candidate => candidate.project.Path == target.Path).index;
        return state with
        {
            Browser = state.Browser with
            {
                Focus = BrowserFocus.Todos,
                ProjectIndex = Math.Max(0, targetIndex),
                TodoIndex = 0,
                PendingTodoSelection = null,
                Error = null
            }
        };
    }

    private static TodoMutationResult ApplyBrowserOperation(
        BrowserTransition transition,
        ProjectCatalog expectedCatalog,
        ProjectTodoMutationService? service,
        DateOnly today)
    {
        if (service is null || transition.ProjectPath is null)
        {
            return TodoMutationResult.Failure("Todo writing is unavailable.");
        }

        if (transition.Operation == BrowserOperation.Create && transition.Update is not null)
        {
            return service.Create(transition.ProjectPath, transition.Update);
        }

        if (transition.Operation == BrowserOperation.RollProjectToday)
        {
            var expectedProject = expectedCatalog.Projects.FirstOrDefault(
                project => project.Path == transition.ProjectPath);
            return expectedProject is null
                ? TodoMutationResult.Failure("The selected project cannot be found.")
                : service.RollOverdueToDate(transition.ProjectPath, expectedProject, today);
        }

        var expected = FindTodo(expectedCatalog, transition.TodoIdentity);
        if (expected is null)
        {
            return TodoMutationResult.Failure("The selected todo cannot be found.");
        }

        return transition.Operation switch
        {
            BrowserOperation.Update when transition.Update is not null =>
                service.UpdateTask(transition.ProjectPath, expected, transition.Update),
            BrowserOperation.ToggleCompleted =>
                service.SetCompleted(transition.ProjectPath, expected, !expected.IsCompleted),
            _ => TodoMutationResult.Failure("The requested todo change is invalid.")
        };
    }

    private static TodoItem? FindTodo(ProjectCatalog catalog, TodoIdentity? identity)
    {
        if (identity is null)
        {
            return null;
        }

        var project = catalog.Projects.FirstOrDefault(candidate => candidate.Path == identity.ProjectPath);
        return project is null
            ? null
            : Flatten(project.Todos).FirstOrDefault(todo => todo.SourceLine == identity.SourceLine);
    }

    private ApplicationState ToggleTimer(
        ApplicationState state,
        BrowserView? browser,
        PlannerView? planner,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration)
    {
        if (configuration.Timer is null)
        {
            return TimerFailure(state, "Task timing requires a [timer] notes_directory configuration.");
        }

        var target = BuildTimerTarget(browser, planner, catalog, state.Tabs.ActiveTab);
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
                return TimerFailure(state, "Could not write the active task timer.");
            }

            var result = weeklyTimeLogService.Record(
                state.Timer,
                state.Timer.RecordingEnd(nowProvider()),
                configuration.Timer);
            if (!result.Succeeded)
            {
                return TimerFailure(state, result.Error ?? "Could not write task time.");
            }

            state = state with { Timer = null };
            if (activeWasPomodoro || target is null || target.TodoIdentity == activeIdentity)
            {
                return state;
            }
        }

        if (target is null)
        {
            return TimerFailure(state, "Select a todo before starting the timer.");
        }

        if (weeklyTimeLogService is null)
        {
            return TimerFailure(state, "Task timing is unavailable.");
        }

        return state with { Timer = new ActiveTimer(target.TodoIdentity, target.ProjectTitle, target.TodoTitle, nowProvider()) };
    }

    private ApplicationState OpenPomodoroPrompt(
        ApplicationState state,
        BrowserView? browser,
        PlannerView? planner,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        bool untracked)
    {
        if (configuration.Timer is null)
        {
            return TimerFailure(state, "Pomodoro timing requires a [timer] configuration.");
        }

        if (state.Timer is not null)
        {
            return TimerFailure(state, "Stop the active timer before starting a Pomodoro.");
        }

        var target = untracked
            ? null
            : BuildTimerTarget(browser, planner, catalog, state.Tabs.ActiveTab);
        if (target is not null && weeklyTimeLogService is null)
        {
            return TimerFailure(state, "Task timing is unavailable.");
        }

        var duration = target?.Duration ?? configuration.Timer.PomodoroDuration;
        var label = target is null
            ? "POMODORO MINUTES"
            : $"POMODORO MINUTES · {target.TodoTitle}";
        return state with
        {
            PomodoroPrompt = new PomodoroPromptState(
                TextBox.Create(
                    label,
                    true,
                    ((int)duration.TotalMinutes).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    true),
                target?.TodoIdentity,
                target?.ProjectTitle,
                target?.TodoTitle)
        };
    }

    private ApplicationState ReducePomodoroPrompt(
        ApplicationState state,
        ConsoleKeyInfo key,
        ApplicationConfiguration configuration)
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
            return state with
            {
                PomodoroPrompt = prompt with { Input = nextInput, Error = null }
            };
        }

        if (!int.TryParse(nextInput.Text, out var minutes) || minutes is < 1 or > 960)
        {
            return state with
            {
                PomodoroPrompt = prompt with
                {
                    Input = nextInput,
                    Error = "Enter a whole number from 1 through 960."
                }
            };
        }

        var target = prompt.IsTaskLinked
            ? new TimerTarget(prompt.TodoIdentity!, prompt.ProjectTitle!, prompt.TodoTitle!, null)
            : null;
        return StartPomodoroImmediately(
            state with { PomodoroPrompt = null },
            target,
            TimeSpan.FromMinutes(minutes),
            configuration);
    }

    private ApplicationState StartPomodoroCommand(
        ApplicationState state,
        BrowserView? browser,
        PlannerView? planner,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ApplicationCommandTransition command)
    {
        if (configuration.Timer is null)
        {
            return TimerFailure(state, "Pomodoro timing requires a [timer] configuration.");
        }

        if (state.Timer is not null)
        {
            return TimerFailure(state, "Stop the active timer before starting a Pomodoro.");
        }

        var selectedTarget = BuildTimerTarget(browser, planner, catalog, state.Tabs.ActiveTab);
        if (command.PomodoroDurationSource == PomodoroDurationSource.SelectedTask)
        {
            if (selectedTarget is null)
            {
                return TimerFailure(state, "Select a todo with a duration before using :pomodoro task.");
            }

            if (selectedTarget.Duration is null)
            {
                return TimerFailure(state, "The selected todo has no ⏱ duration.");
            }

            return StartPomodoroImmediately(state, selectedTarget, selectedTarget.Duration.Value, configuration);
        }

        var duration = TimeSpan.FromMinutes(command.PomodoroMinutes!.Value);
        var target = command.PomodoroUntracked ? null : selectedTarget;
        return StartPomodoroImmediately(state, target, duration, configuration);
    }

    private ApplicationState StartPomodoroImmediately(
        ApplicationState state,
        TimerTarget? target,
        TimeSpan duration,
        ApplicationConfiguration configuration)
    {
        if (configuration.Timer is null)
        {
            return TimerFailure(state, "Pomodoro timing requires a [timer] configuration.");
        }

        if (state.Timer is not null)
        {
            return TimerFailure(state, "Stop the active timer before starting a Pomodoro.");
        }

        if (target is not null && weeklyTimeLogService is null)
        {
            return TimerFailure(state, "Task timing is unavailable.");
        }

        return state with
        {
            Timer = new ActiveTimer(
                target?.TodoIdentity,
                target?.ProjectTitle,
                target?.TodoTitle,
                nowProvider(),
                duration),
            PomodoroPrompt = null
        };
    }

    private ApplicationState CompletePomodoro(ApplicationState state, ApplicationConfiguration configuration)
    {
        if (state.Timer is not { IsPomodoro: true, CompletionHandled: false } timer ||
            !timer.IsComplete(nowProvider()))
        {
            return state;
        }

        state = state with { Timer = timer with { CompletionHandled = true } };
        var completion = new PomodoroCompletion(
            timer.TodoTitle,
            timer.Duration ?? TimeSpan.Zero,
            nowProvider());
        pomodoroCompletionNotifier?.Notify(completion, configuration.Timer?.Bell != false);
        if (pomodoroCompletionNotifier is null && configuration.Timer?.Bell != false)
            terminalUi.RingBell();

        return StopTimer(state, configuration) with { PomodoroCompletion = completion };
    }

    private ApplicationState StopTimer(ApplicationState state, ApplicationConfiguration configuration)
    {
        if (state.Timer is null) return state;
        if (!state.Timer.IsTaskLinked) return state with { Timer = null };
        if (configuration.Timer is null || weeklyTimeLogService is null)
            return TimerFailure(state, "Could not write the active task timer.");
        var result = weeklyTimeLogService.Record(
            state.Timer,
            state.Timer.RecordingEnd(nowProvider()),
            configuration.Timer);
        return result.Succeeded ? state with { Timer = null } : TimerFailure(state, result.Error ?? "Could not write task time.");
    }

    private static TimerTarget? BuildTimerTarget(BrowserView? browser, PlannerView? planner, ProjectCatalog catalog, TabId tab)
    {
        if (tab == TodosTab && browser?.SelectedTodoIdentity is { } identity && browser.SelectedTodo is { } todo)
        {
            var project = catalog.Projects.FirstOrDefault(candidate => candidate.Path == identity.ProjectPath);
            return project is null ? null : new TimerTarget(identity, project.Title, todo.Title, todo.Duration);
        }

        return tab == PlannerTab && planner?.SelectedFocusedAssignment is { } assignment
            ? new TimerTarget(
                assignment.Identity,
                assignment.ProjectTitle,
                assignment.Todo.Title,
                assignment.Todo.Duration)
            : null;
    }

    private static ApplicationState TimerFailure(ApplicationState state, string error) => state.Tabs.ActiveTab == TodosTab
        ? state with { Browser = state.Browser with { Error = error } }
        : state with { Planner = state.Planner with { Error = error } };

    private string? TimerStatus(ActiveTimer? timer)
    {
        if (timer is null) return null;
        if (timer.IsPomodoro)
        {
            var remaining = timer.Remaining(nowProvider());
            var totalSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
            var countdown = totalSeconds >= 3600
                ? $"{totalSeconds / 3600:00}:{totalSeconds % 3600 / 60:00}:{totalSeconds % 60:00}"
                : $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            var title = timer.TodoTitle is null ? string.Empty : $" · {timer.TodoTitle}";
            return $"POMODORO {countdown}{title}";
        }

        var elapsed = timer.Elapsed(nowProvider());
        return $"TIMER {((int)elapsed.TotalHours):00}:{elapsed.Minutes:00} · {timer.TodoTitle}";
    }

    private bool TimerIsBright(ActiveTimer? timer) => timer is not null && nowProvider().Second % 2 == 0;

    private static PlannerFocusBlock? ActiveFocusBlock(ActiveTimer? timer) =>
        timer is { IsPomodoro: true, EndsAt: { } endsAt }
            ? new PlannerFocusBlock(timer.StartedAt, endsAt, timer.TodoTitle)
            : null;

    private sealed record TimerTarget(
        TodoIdentity TodoIdentity,
        string ProjectTitle,
        string TodoTitle,
        TimeSpan? Duration);

    private static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var subtask in Flatten(todo.Subtasks))
            {
                yield return subtask;
            }
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
