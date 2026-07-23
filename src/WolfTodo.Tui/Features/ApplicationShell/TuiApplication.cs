using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
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
    Func<DateOnly>? todayProvider = null)
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
                            actionCatalog.Create(true, browserView, null, configuration.KeyBindings));
                    }
                    var renderedBrowserView = browserView with
                    {
                        GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                        GlobalError = state.Command.Error,
                        CommandPalette = paletteView
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
                    plannerView = plannerPresenter.CreateView(catalog, state.Planner, agenda, configuration.Planner);
                    state = state with { Planner = plannerView.State };
                    if (state.Palette.IsOpen)
                    {
                        paletteView = palettePresenter.CreateView(
                            state.Palette,
                            actionCatalog.Create(false, null, plannerView, configuration.KeyBindings));
                    }
                    var renderedPlannerView = plannerView with
                    {
                        GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                        GlobalError = state.Command.Error,
                        CommandPalette = paletteView
                    };
                    terminalUi.ShowPlanner(
                        tabView,
                        renderedPlannerView,
                        configuration.KeyBindings,
                        configuration.Theme);
                }

                var pendingKey = state.Tabs.ActiveTab == PlannerTab
                    ? terminalUi.ReadKey(plannerCalendarCache.IsRefreshing
                        ? TimeSpan.FromMilliseconds(250)
                        : TimeSpan.FromMinutes(1))
                    : terminalUi.ReadKey();
                if (pendingKey is null)
                {
                    continue;
                }

                var key = pendingKey.Value;
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
                        return 0;
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
                            configuration.KeyBindings));
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
                        return 0;
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
