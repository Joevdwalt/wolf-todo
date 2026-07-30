using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationTransitionExecutor(
    ProjectCatalogLoader catalogLoader,
    ITerminalUi terminalUi,
    Func<DateOnly> todayProvider,
    ProjectTodoMutationService? mutationService,
    IExternalEditorLauncher? externalEditorLauncher,
    DayScheduleExportService? exportService)
{
    public ApplicationRuntime ApplyBrowser(
        ApplicationRuntime runtime,
        BrowserTransition transition)
    {
        runtime = runtime with { State = runtime.State with { Browser = transition.State } };
        if (transition.Operation == BrowserOperation.None)
        {
            return runtime;
        }

        return transition.Operation == BrowserOperation.EditExternal
            ? ApplyBrowserExternalEdit(runtime, transition)
            : ApplyBrowserMutation(runtime, transition);
    }

    public ApplicationRuntime ApplyPlanner(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        runtime = runtime with { State = runtime.State with { Planner = transition.State } };
        if (transition.Operation == PlannerOperation.None)
        {
            return runtime;
        }

        return transition.Operation == PlannerOperation.EditExternal
            ? ApplyPlannerExternalEdit(runtime, transition)
            : ApplyPlannerMutation(runtime, transition);
    }

    public ApplicationRuntime MoveTodoToProject(
        ApplicationRuntime runtime,
        BrowserView? view,
        string? targetTitle)
    {
        if (runtime.State.Tabs.ActiveTab != ApplicationTabs.Todos ||
            view?.SelectedTodoIdentity is not { } identity)
        {
            return BrowserFailure(runtime, "Select a todo in the Todos tab before moving it.");
        }

        var target = runtime.Catalog.Projects.FirstOrDefault(project =>
            string.Equals(project.Title, targetTitle, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return BrowserFailure(runtime, $"Project not found: {targetTitle}");
        }

        var source = runtime.Catalog.Projects.FirstOrDefault(project => project.Path == identity.ProjectPath);
        var todo = source is null ? null : TodoCatalogLookup.Find(source.Todos, identity.SourceLine);
        if (todo is null || mutationService is null)
        {
            return BrowserFailure(runtime, "The selected todo cannot be moved.");
        }

        var result = mutationService.Move(source!.Path, target.Path, todo);
        return result.Succeeded
            ? SelectMovedProject(Reload(runtime), target.Path)
            : BrowserFailure(runtime, result.Error ?? "The selected todo cannot be moved.");
    }

    public ApplicationRuntime ExportDaySchedule(ApplicationRuntime runtime, PlannerView view)
    {
        if (exportService is null)
        {
            return PlannerFailure(runtime, "Day schedule export is unavailable.");
        }

        var result = exportService.Export(view, runtime.Configuration.Planner.Export);
        return result.Succeeded
            ? runtime with
            {
                State = runtime.State with
                {
                    Planner = runtime.State.Planner with
                    {
                        Error = $"Exported day schedule to {result.Path}"
                    }
                }
            }
            : PlannerFailure(runtime, result.Error ?? "Could not export day schedule.");
    }

    private ApplicationRuntime ApplyBrowserMutation(
        ApplicationRuntime runtime,
        BrowserTransition transition)
    {
        var expectedCatalog = runtime.Catalog;
        runtime = Reload(runtime);
        var result = ExecuteBrowserMutation(transition, expectedCatalog);
        var browser = runtime.State.Browser with
        {
            Error = result.Error,
            Editor = result.Succeeded
                ? null
                : runtime.State.Browser.Editor is null
                    ? null
                    : runtime.State.Browser.Editor with { Error = result.Error },
            PendingTodoSelection = PendingBrowserSelection(transition, result)
        };
        var updated = runtime with { State = runtime.State with { Browser = browser } };
        return result.Succeeded ? Reload(updated) : updated;
    }

    private TodoMutationResult ExecuteBrowserMutation(
        BrowserTransition transition,
        ProjectCatalog expectedCatalog)
    {
        if (mutationService is null || transition.ProjectPath is null)
        {
            return TodoMutationResult.Failure("Todo writing is unavailable.");
        }

        if (transition.Operation == BrowserOperation.Create && transition.Update is not null)
        {
            return mutationService.Create(transition.ProjectPath, transition.Update);
        }

        if (transition.Operation == BrowserOperation.RollProjectToday)
        {
            var project = expectedCatalog.Projects.FirstOrDefault(
                candidate => candidate.Path == transition.ProjectPath);
            return project is null
                ? TodoMutationResult.Failure("The selected project cannot be found.")
                : mutationService.RollOverdueToDate(transition.ProjectPath, project, todayProvider());
        }

        var expected = TodoCatalogLookup.Find(expectedCatalog, transition.TodoIdentity);
        if (expected is null)
        {
            return TodoMutationResult.Failure("The selected todo cannot be found.");
        }

        return transition.Operation switch
        {
            BrowserOperation.Update when transition.Update is not null =>
                mutationService.UpdateTask(transition.ProjectPath, expected, transition.Update),
            BrowserOperation.ToggleCompleted =>
                mutationService.SetCompleted(transition.ProjectPath, expected, !expected.IsCompleted),
            _ => TodoMutationResult.Failure("The requested todo change is invalid.")
        };
    }

    private ApplicationRuntime ApplyPlannerMutation(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        if (mutationService is null)
        {
            return PlannerFailure(runtime, "Todo writing is unavailable.");
        }

        var expected = TodoCatalogLookup.Find(runtime.Catalog, transition.TodoIdentity);
        runtime = Reload(runtime);
        return transition.Operation == PlannerOperation.Create
            ? CreatePlannerTodo(runtime, transition)
            : UpdatePlannerTodo(runtime, transition, expected);
    }

    private ApplicationRuntime CreatePlannerTodo(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        if (transition.ProjectPath is null || transition.Update is null)
        {
            return PlannerFailure(runtime, "The new todo is incomplete.");
        }

        if (transition.Update.Fields.Schedule is null)
        {
            return PlannerFailure(runtime, "A schedule is required when creating from Planner.");
        }

        var result = mutationService!.Create(transition.ProjectPath, transition.Update);
        if (!result.Succeeded)
        {
            return PlannerFailure(runtime, result.Error ?? "The todo could not be created.");
        }

        var identity = result.SourceLine is { } line
            ? new TodoIdentity(transition.ProjectPath, line)
            : null;
        return PlannerSuccess(Reload(runtime), transition.Update.Fields.Schedule, identity);
    }

    private ApplicationRuntime UpdatePlannerTodo(
        ApplicationRuntime runtime,
        PlannerTransition transition,
        TodoItem? expected)
    {
        if (transition.TodoIdentity is null)
        {
            return PlannerFailure(runtime, "The selected todo cannot be updated.");
        }

        if (expected is null)
        {
            return PlannerFailure(runtime, "The selected todo cannot be found.");
        }

        var schedule = PlannerSchedule(runtime.State.Planner, transition.ScheduleTarget);
        var result = ExecutePlannerMutation(transition, expected, schedule);
        if (!result.Succeeded)
        {
            return PlannerFailure(runtime, result.Error ?? "The selected todo could not be updated.");
        }

        var followed = transition.Operation switch
        {
            PlannerOperation.Schedule => schedule,
            PlannerOperation.Update => transition.Update?.Fields.Schedule,
            _ => null
        };
        return PlannerSuccess(
            Reload(runtime),
            followed,
            followed is null ? null : transition.TodoIdentity);
    }

    private TodoMutationResult ExecutePlannerMutation(
        PlannerTransition transition,
        TodoItem expected,
        TodoSchedule schedule) => transition.Operation switch
    {
        PlannerOperation.Schedule => mutationService!.SetSchedule(
            transition.TodoIdentity!.ProjectPath, expected, schedule),
        PlannerOperation.Unschedule => mutationService!.SetSchedule(
            transition.TodoIdentity!.ProjectPath, expected, null),
        PlannerOperation.Update when transition.Update is not null => mutationService!.UpdateTask(
            transition.TodoIdentity!.ProjectPath, expected, transition.Update),
        PlannerOperation.ToggleCompleted => mutationService!.SetCompleted(
            transition.TodoIdentity!.ProjectPath, expected, !expected.IsCompleted),
        _ => TodoMutationResult.Failure("The requested planner change is invalid.")
    };

    private ApplicationRuntime ApplyBrowserExternalEdit(
        ApplicationRuntime runtime,
        BrowserTransition transition)
    {
        var result = OpenExternal(transition.ProjectPath, transition.TodoIdentity);
        var updated = runtime with
        {
            State = runtime.State with
            {
                Browser = runtime.State.Browser with
                {
                    PendingTodoSelection = null,
                    Error = result.Error
                }
            }
        };
        return result.Started ? Reload(updated) : updated;
    }

    private ApplicationRuntime ApplyPlannerExternalEdit(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        var result = OpenExternal(transition.ProjectPath, transition.TodoIdentity);
        var updated = result.Error is null
            ? PlannerSuccess(runtime)
            : PlannerFailure(runtime, result.Error);
        return result.Started ? Reload(updated) : updated;
    }

    private ExternalEditorResult OpenExternal(string? path, TodoIdentity? identity)
    {
        if (externalEditorLauncher is null || path is null || identity is null)
        {
            return ExternalEditorResult.Failure(false, "External editing is unavailable.");
        }

        terminalUi.SuspendForExternalProcess();
        try
        {
            return externalEditorLauncher.Open(path, identity.SourceLine);
        }
        finally
        {
            terminalUi.ResumeAfterExternalProcess();
        }
    }

    private ApplicationRuntime Reload(ApplicationRuntime runtime) =>
        runtime with { Catalog = catalogLoader.Load(runtime.Configuration.ProjectFiles) };

    private static TodoIdentity? PendingBrowserSelection(
        BrowserTransition transition,
        TodoMutationResult result) =>
        result.Succeeded && result.SourceLine is not null && transition.ProjectPath is not null
            ? new TodoIdentity(transition.ProjectPath, result.SourceLine.Value)
            : result.Succeeded && transition.Operation == BrowserOperation.RollProjectToday
                ? transition.TodoIdentity
                : null;

    private static TodoSchedule PlannerSchedule(
        PlannerState state,
        PlannerScheduleTarget target) =>
        target == PlannerScheduleTarget.AllDay
            ? new TodoSchedule(state.SelectedDate)
            : new TodoSchedule(
                state.SelectedDate,
                new TimeOnly(6, 0).AddMinutes(state.SlotIndex * 15));

    private static ApplicationRuntime PlannerSuccess(
        ApplicationRuntime runtime,
        TodoSchedule? follow = null,
        TodoIdentity? identity = null) =>
        runtime with
        {
            State = runtime.State with
            {
                Planner = runtime.State.Planner with
                {
                    SelectedDate = follow?.Date ?? runtime.State.Planner.SelectedDate,
                    Focus = follow is null
                        ? runtime.State.Planner.Focus
                        : follow.Time is null ? PlannerFocus.AllDay : PlannerFocus.Timeline,
                    SlotIndex = follow?.Time is { } time
                        ? ((time.Hour - 6) * 4) + (time.Minute / 15)
                        : runtime.State.Planner.SlotIndex,
                    PendingAllDaySelection = follow?.Time is null ? identity : null,
                    Mode = PlannerMode.Browse,
                    MovingTodo = null,
                    Editor = null,
                    Error = null
                }
            }
        };

    private static ApplicationRuntime PlannerFailure(ApplicationRuntime runtime, string error) =>
        runtime with
        {
            State = runtime.State with
            {
                Planner = runtime.State.Planner with
                {
                    Error = error,
                    Editor = runtime.State.Planner.Editor is null
                        ? null
                        : runtime.State.Planner.Editor with { Error = error }
                }
            }
        };

    private static ApplicationRuntime BrowserFailure(ApplicationRuntime runtime, string error) =>
        runtime with
        {
            State = runtime.State with
            {
                Browser = runtime.State.Browser with { Error = error }
            }
        };

    private static ApplicationRuntime SelectMovedProject(ApplicationRuntime runtime, string targetPath)
    {
        var index = runtime.Catalog.Projects
            .Select((project, candidateIndex) => (project, candidateIndex))
            .First(candidate => candidate.project.Path == targetPath)
            .candidateIndex;
        return runtime with
        {
            State = runtime.State with
            {
                Browser = runtime.State.Browser with
                {
                    Focus = BrowserFocus.Todos,
                    ProjectIndex = Math.Max(0, index),
                    TodoIndex = 0,
                    PendingTodoSelection = null,
                    Error = null
                }
            }
        };
    }
}
