using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell.ExternalEditing;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class PlannerTransitionExecutor(
    ProjectCatalogLoader catalogLoader,
    ProjectTodoMutationService? mutationService,
    ExternalTodoEditorExecutor externalEditor,
    DayScheduleExportService? exportService)
{
    public ApplicationRuntime Apply(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        runtime = runtime with { State = runtime.State with { Planner = transition.State } };
        if (transition.Operation == PlannerOperation.None)
        {
            return runtime;
        }

        return transition.Operation == PlannerOperation.EditExternal
            ? ApplyExternalEdit(runtime, transition)
            : ApplyMutation(runtime, transition);
    }

    public ApplicationRuntime ExportDaySchedule(ApplicationRuntime runtime, PlannerView view)
    {
        if (exportService is null)
        {
            return Failure(runtime, "Day schedule export is unavailable.");
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
            : Failure(runtime, result.Error ?? "Could not export day schedule.");
    }

    private ApplicationRuntime ApplyMutation(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        if (mutationService is null)
        {
            return Failure(runtime, "Todo writing is unavailable.");
        }

        var expected = TodoCatalogLookup.Find(runtime.Catalog, transition.TodoIdentity);
        runtime = Reload(runtime);
        return transition.Operation == PlannerOperation.Create
            ? CreateTodo(runtime, transition)
            : UpdateTodo(runtime, transition, expected);
    }

    private ApplicationRuntime CreateTodo(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        if (transition.ProjectPath is null || transition.Update is null)
        {
            return Failure(runtime, "The new todo is incomplete.");
        }

        if (transition.Update.Fields.Schedule is null)
        {
            return Failure(runtime, "A schedule is required when creating from Planner.");
        }

        var result = mutationService!.Create(transition.ProjectPath, transition.Update);
        if (!result.Succeeded)
        {
            return Failure(runtime, result.Error ?? "The todo could not be created.");
        }

        var identity = result.SourceLine is { } line
            ? new TodoIdentity(transition.ProjectPath, line)
            : null;
        return Success(Reload(runtime), transition.Update.Fields.Schedule, identity);
    }

    private ApplicationRuntime UpdateTodo(
        ApplicationRuntime runtime,
        PlannerTransition transition,
        TodoItem? expected)
    {
        if (transition.TodoIdentity is null)
        {
            return Failure(runtime, "The selected todo cannot be updated.");
        }

        if (expected is null)
        {
            return Failure(runtime, "The selected todo cannot be found.");
        }

        var schedule = PlannerSchedule(runtime.State.Planner, transition.ScheduleTarget);
        var result = ExecuteMutation(transition, expected, schedule);
        if (!result.Succeeded)
        {
            return Failure(runtime, result.Error ?? "The selected todo could not be updated.");
        }

        var followed = transition.Operation switch
        {
            PlannerOperation.Schedule => schedule,
            PlannerOperation.Update => transition.Update?.Fields.Schedule,
            _ => null
        };
        return Success(
            Reload(runtime),
            followed,
            followed is null ? null : transition.TodoIdentity);
    }

    private TodoMutationResult ExecuteMutation(
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

    private ApplicationRuntime ApplyExternalEdit(
        ApplicationRuntime runtime,
        PlannerTransition transition)
    {
        var result = externalEditor.Open(transition.ProjectPath, transition.TodoIdentity);
        var updated = result.Error is null
            ? Success(runtime)
            : Failure(runtime, result.Error);
        return result.Started ? Reload(updated) : updated;
    }

    private ApplicationRuntime Reload(ApplicationRuntime runtime) =>
        runtime with { Catalog = catalogLoader.Load(runtime.Configuration.ProjectFiles) };

    private static TodoSchedule PlannerSchedule(
        PlannerState state,
        PlannerScheduleTarget target) =>
        target == PlannerScheduleTarget.AllDay
            ? new TodoSchedule(state.SelectedDate)
            : new TodoSchedule(
                state.SelectedDate,
                new TimeOnly(6, 0).AddMinutes(state.SlotIndex * 15));

    private static ApplicationRuntime Success(
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

    private static ApplicationRuntime Failure(ApplicationRuntime runtime, string error) =>
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
}
