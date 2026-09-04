using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class PlannerWorkflow(
    DayPlannerPresenter presenter,
    DayPlannerReducer reducer,
    PlannerCalendarAgendaCache calendarCache,
    DayScheduleExportService? dayScheduleExportService,
    IExternalEditorLauncher? externalEditorLauncher,
    ITerminalUi terminalUi,
    ProjectCatalogLoader catalogLoader)
{
    public bool IsRefreshing => calendarCache.IsRefreshing;

    public PlannerView CreateView(
        ProjectCatalog catalog,
        PlannerState state,
        ApplicationConfiguration configuration,
        PlannerFocusBlock? activeFocusBlock) => presenter.CreateView(
        catalog,
        state,
        calendarCache.GetAgenda(configuration.GoogleCalendar, state.SelectedDate),
        configuration.Planner,
        activeFocusBlock);

    public void Refresh(ApplicationConfiguration configuration, PlannerState state) =>
        calendarCache.Refresh(configuration.GoogleCalendar, state.SelectedDate);

    public PlannerTransition Reduce(
        PlannerState state,
        ConsoleKeyInfo key,
        ApplicationConfiguration configuration,
        PlannerView view) => reducer.Reduce(
        state,
        key,
        configuration.KeyBindings,
        view,
        configuration.Planner.DefaultDuration);

    public PlannerTransition ReduceAction(
        PlannerState state,
        PlannerAction action,
        ApplicationConfiguration configuration,
        PlannerView view) => reducer.ReduceAction(
        state,
        action,
        view,
        configuration.Planner.DefaultDuration);

    public ApplicationState Export(
        ApplicationState state,
        PlannerView view,
        ApplicationConfiguration configuration)
    {
        if (dayScheduleExportService is null)
        {
            return Failure(state, "Day schedule export is unavailable.");
        }

        var result = dayScheduleExportService.Export(view, configuration.Planner.Export);
        return result.Succeeded
            ? state with
            {
                Planner = state.Planner with { Error = $"Exported day schedule to {result.Path}" }
            }
            : Failure(state, result.Error ?? "Could not export day schedule.");
    }

    public (ApplicationState State, ProjectCatalog Catalog) ApplyTransition(
        ApplicationState state,
        PlannerTransition transition,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? mutationService)
    {
        state = state with { Planner = transition.State };
        if (transition.Operation == PlannerOperation.None)
        {
            return (state, catalog);
        }

        if (transition.Operation == PlannerOperation.EditExternal)
        {
            if (externalEditorLauncher is null ||
                transition.ProjectPath is null ||
                transition.TodoIdentity is null)
            {
                return (Failure(state, "External editing is unavailable."), catalog);
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
                ? (Success(state), catalog)
                : (Failure(state, externalResult.Error), catalog);
        }

        if (mutationService is null)
        {
            return (Failure(state, "Todo writing is unavailable."), catalog);
        }

        var expected = FindTodo(catalog, transition.TodoIdentity);
        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var schedule = transition.ScheduleTarget == PlannerScheduleTarget.AllDay
            ? new TodoSchedule(state.Planner.SelectedDate)
            : new TodoSchedule(
                state.Planner.SelectedDate,
                new TimeOnly(6, 0).AddMinutes(state.Planner.SlotIndex * 15));

        if (transition.Operation == PlannerOperation.Create)
        {
            if (transition.ProjectPath is null || transition.Update is null)
            {
                return (Failure(state, "The new todo is incomplete."), catalog);
            }

            if (transition.Update.Fields.Schedule is null)
            {
                return (Failure(state, "A schedule is required when creating from Planner."), catalog);
            }

            var created = mutationService.Create(transition.ProjectPath, transition.Update);
            if (!created.Succeeded)
            {
                return (Failure(state, created.Error ?? "The todo could not be created."), catalog);
            }

            catalog = catalogLoader.Load(configuration.ProjectFiles);
            return (
                Success(
                    state,
                    transition.Update.Fields.Schedule,
                    created.SourceLine is { } createdLine
                        ? new TodoIdentity(transition.ProjectPath, createdLine)
                        : null),
                catalog);
        }

        if (transition.TodoIdentity is null)
        {
            return (Failure(state, "The selected todo cannot be updated."), catalog);
        }

        if (expected is null)
        {
            return (Failure(state, "The selected todo cannot be found."), catalog);
        }

        var result = transition.Operation switch
        {
            PlannerOperation.Schedule => mutationService.SetSchedule(
                transition.TodoIdentity.ProjectPath,
                expected,
                schedule),
            PlannerOperation.Unschedule => mutationService.SetSchedule(
                transition.TodoIdentity.ProjectPath,
                expected,
                null),
            PlannerOperation.Update when transition.Update is not null => mutationService.UpdateTask(
                transition.TodoIdentity.ProjectPath,
                expected,
                transition.Update),
            PlannerOperation.ToggleCompleted => mutationService.SetCompleted(
                transition.TodoIdentity.ProjectPath,
                expected,
                !expected.IsCompleted),
            _ => TodoMutationResult.Failure("The requested planner change is invalid.")
        };
        if (!result.Succeeded)
        {
            return (Failure(state, result.Error ?? "The selected todo could not be updated."), catalog);
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var followedSchedule = transition.Operation switch
        {
            PlannerOperation.Schedule => schedule,
            PlannerOperation.Update => transition.Update?.Fields.Schedule,
            _ => null
        };
        return (
            Success(
                state,
                followedSchedule,
                followedSchedule is null ? null : transition.TodoIdentity),
            catalog);
    }

    private static ApplicationState Success(
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

    private static ApplicationState Failure(ApplicationState state, string error) => state with
    {
        Planner = state.Planner with
        {
            Error = error,
            Editor = state.Planner.Editor is null ? null : state.Planner.Editor with { Error = error }
        }
    };

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
}
