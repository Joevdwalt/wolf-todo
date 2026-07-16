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
    ApplicationActionCatalog? actionCatalog = null)
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
    private readonly ApplicationActionCatalog actionCatalog = actionCatalog ?? new ApplicationActionCatalog();

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
        var selectedProjectPath = applicationStateStore.LoadSelectedProjectPath();
        var initialProjectIndex = FindProjectIndex(catalog, selectedProjectPath);
        var browserState = BrowserState.Initial with { ProjectIndex = initialProjectIndex };
        var state = new ApplicationState(TabHostState.CreateInitial(Tabs), browserState)
        {
            Planner = PlannerState.CreateInitial(DateOnly.FromDateTime(DateTime.Today))
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
                    browserView = browserPresenter.CreateView(catalog, state.Browser);
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
                    plannerView = plannerPresenter.CreateView(catalog, state.Planner);
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

                var key = terminalUi.ReadKey();
                var featureCapturesInput = state.Tabs.ActiveTab == TodosTab
                    ? state.Browser.IsFilterMode || state.Browser.IsSortMode ||
                      state.Browser.Form is not null || state.Browser.ContentEditor is not null
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
                            ApplicationActionId.BrowserEditContent => BrowserAction.EditContent,
                            ApplicationActionId.BrowserToggleCompleted => BrowserAction.ToggleCompleted,
                            ApplicationActionId.BrowserToggleDetails => BrowserAction.ToggleDetails,
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
                                browserView!,
                                ref catalog,
                                configuration,
                                mutationService);
                        }

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
                        _ => (PlannerAction?)null
                    };
                    if (plannerAction is not null)
                    {
                        var transition = plannerReducer.ReduceAction(
                            state.Planner,
                            plannerAction.Value,
                            plannerView!);
                        state = state with { Planner = transition.State };
                        if (transition.Operation == PlannerOperation.Unschedule &&
                            transition.TodoIdentity is not null && mutationService is not null)
                        {
                            var assignment = FindAssignment(plannerView!, transition.TodoIdentity);
                            if (assignment is not null)
                            {
                                var result = mutationService.SetSchedule(
                                    assignment.ProjectPath,
                                    assignment.Todo,
                                    null);
                                state = state with
                                {
                                    Planner = state.Planner with { Error = result.Error }
                                };
                                if (result.Succeeded)
                                {
                                    catalog = catalogLoader.Load(configuration.ProjectFiles);
                                }
                            }
                        }
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
                    var transition = plannerReducer.Reduce(
                        state.Planner,
                        key,
                        configuration.KeyBindings,
                        plannerView!);
                    state = state with { Planner = transition.State };
                    if (transition.Operation != PlannerOperation.None)
                    {
                        if (transition.Operation == PlannerOperation.Create &&
                            transition.ProjectPath is not null &&
                            transition.Update is not null &&
                            mutationService is not null)
                        {
                            var createSchedule = new TodoSchedule(
                                state.Planner.SelectedDate,
                                new TimeOnly(6, 0).AddMinutes(state.Planner.SlotIndex * 30));
                            var latestCatalog = catalogLoader.Load(configuration.ProjectFiles);
                            if (latestCatalog.Projects
                                .SelectMany(project => Flatten(project.Todos))
                                .Any(todo => todo.Schedule == createSchedule))
                            {
                                catalog = latestCatalog;
                                state = state with
                                {
                                    Planner = state.Planner with
                                    {
                                        Mode = PlannerMode.Browse,
                                        Error = "That timeslot is already occupied."
                                    }
                                };
                                continue;
                            }

                            var created = mutationService.Create(
                                transition.ProjectPath,
                                transition.Update,
                                createSchedule);
                            state = state with
                            {
                                Planner = state.Planner with
                                {
                                    Mode = PlannerMode.Browse,
                                    Error = created.Error
                                }
                            };
                            if (created.Succeeded)
                            {
                                catalog = catalogLoader.Load(configuration.ProjectFiles);
                            }

                            continue;
                        }

                        if (transition.TodoIdentity is null)
                        {
                            state = state with
                            {
                                Planner = state.Planner with { Error = "The selected todo cannot be updated." }
                            };
                            continue;
                        }

                        var assignment = FindAssignment(plannerView!, transition.TodoIdentity);
                        if (assignment is null || mutationService is null)
                        {
                            state = state with
                            {
                                Planner = state.Planner with { Error = "The selected todo cannot be updated." }
                            };
                            continue;
                        }

                        var schedule = transition.Operation == PlannerOperation.Schedule
                            ? new TodoSchedule(
                                state.Planner.SelectedDate,
                                new TimeOnly(6, 0).AddMinutes(state.Planner.SlotIndex * 30))
                            : null;
                        if (schedule is not null)
                        {
                            var latestCatalog = catalogLoader.Load(configuration.ProjectFiles);
                            var occupied = latestCatalog.Projects
                                .SelectMany(project => Flatten(project.Todos)
                                    .Select(todo => (project.Path, Todo: todo)))
                                .Any(candidate =>
                                    candidate.Todo.Schedule == schedule &&
                                    (candidate.Path != transition.TodoIdentity.ProjectPath ||
                                     candidate.Todo.SourceLine != transition.TodoIdentity.SourceLine));
                            catalog = latestCatalog;
                            if (occupied)
                            {
                                state = state with
                                {
                                    Planner = state.Planner with
                                    {
                                        Mode = PlannerMode.Browse,
                                        MovingTodo = null,
                                        Error = "That timeslot is already occupied."
                                    }
                                };
                                continue;
                            }
                        }

                        var result = mutationService.SetSchedule(
                            assignment.ProjectPath,
                            assignment.Todo,
                            schedule);
                        state = state with
                        {
                            Planner = state.Planner with
                            {
                                MovingTodo = null,
                                Mode = PlannerMode.Browse,
                                Error = result.Error
                            }
                        };
                        if (result.Succeeded)
                        {
                            catalog = catalogLoader.Load(configuration.ProjectFiles);
                        }
                    }

                    continue;
                }

                var browserTransition = browserReducer.Reduce(state.Browser, key, configuration, browserView!);
                state = ApplyBrowserTransition(
                    state,
                    browserTransition,
                    browserView!,
                    ref catalog,
                    configuration,
                    mutationService);
            }
        }
        finally
        {
            applicationStateStore.SaveSelectedProjectPath(selectedProjectPath);
            terminalUi.SetCursorVisible(true);
        }
    }

    private static PlannerAssignment? FindAssignment(PlannerView view, TodoIdentity identity) =>
        view.Slots.SelectMany(slot => slot.Assignments)
            .Concat(view.PickerTodos)
            .FirstOrDefault(assignment => assignment.Identity == identity);

    private ApplicationState ApplyBrowserTransition(
        ApplicationState state,
        BrowserTransition transition,
        BrowserView view,
        ref ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? service)
    {
        state = state with { Browser = transition.State };
        if (transition.Operation == BrowserOperation.None)
        {
            return state;
        }

        var result = ApplyBrowserOperation(transition, view, catalog, service);
        state = state with
        {
            Browser = state.Browser with
            {
                Error = result.Error,
                PendingTodoSelection = result.Succeeded && result.SourceLine is not null &&
                                       transition.ProjectPath is not null
                    ? new TodoIdentity(transition.ProjectPath, result.SourceLine.Value)
                    : null
            }
        };
        if (result.Succeeded)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        return state;
    }

    private static TodoMutationResult ApplyBrowserOperation(
        BrowserTransition transition,
        BrowserView view,
        ProjectCatalog catalog,
        ProjectTodoMutationService? service)
    {
        if (service is null || transition.ProjectPath is null)
        {
            return TodoMutationResult.Failure("Todo writing is unavailable.");
        }

        if (transition.Operation == BrowserOperation.Create && transition.Update is not null)
        {
            return service.Create(transition.ProjectPath, transition.Update);
        }

        var expected = FindTodo(catalog, transition.TodoIdentity);
        if (expected is null)
        {
            return TodoMutationResult.Failure("The selected todo cannot be found.");
        }

        return transition.Operation switch
        {
            BrowserOperation.Update when transition.Update is not null =>
                service.Update(transition.ProjectPath, expected, transition.Update),
            BrowserOperation.UpdateContent when transition.ContentUpdate is not null =>
                service.UpdateContent(transition.ProjectPath, expected, transition.ContentUpdate),
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

    private static int FindProjectIndex(ProjectCatalog catalog, string? selectedProjectPath)
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
                return index + 1;
            }
        }

        for (var index = 0; index < catalog.Errors.Length; index++)
        {
            if (string.Equals(catalog.Errors[index].Path, selectedProjectPath, comparison))
            {
                return catalog.Projects.Length + index + 1;
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
