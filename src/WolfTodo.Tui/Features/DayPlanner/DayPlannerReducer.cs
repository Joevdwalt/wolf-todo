using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayPlannerReducer(Func<DateOnly>? todayProvider = null)
{
    private readonly Func<DateOnly> todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
    private readonly TodoEditorReducer todoEditorReducer = new(todayProvider);

    public PlannerTransition ReduceAction(PlannerState state, PlannerAction action, PlannerView view) =>
        action switch
        {
            PlannerAction.PreviousDay => Transition(state with
            {
                SelectedDate = state.SelectedDate.AddDays(-1),
                Error = null
            }),
            PlannerAction.NextDay => Transition(state with
            {
                SelectedDate = state.SelectedDate.AddDays(1),
                Error = null
            }),
            PlannerAction.Today => Transition(state with { SelectedDate = todayProvider(), Error = null }),
            PlannerAction.Create when view.SelectedSlot.Assignments.Length > 0 => Transition(state with
            {
                Error = "That timeslot is already occupied."
            }),
            PlannerAction.Create when view.Projects.Length > 0 => Transition(state with
            {
                Editor = todoEditorReducer.CreateEditor(null, true, SelectedSchedule(state), true),
                Error = null
            }),
            PlannerAction.Create => Transition(state with { Error = "No valid projects are available." }),
            PlannerAction.ToggleDetails => Transition(state with
            {
                ShowDetails = !state.ShowDetails,
                Error = null
            }),
            PlannerAction.Edit when view.SelectedAssignment is not null =>
                Transition(state with
            {
                Editor = todoEditorReducer.EditEditor(
                    view.SelectedAssignment.Todo,
                    view.SelectedAssignment.Identity),
                Error = null
            }),
            PlannerAction.EditExternal when view.SelectedAssignment is not null => new PlannerTransition(
                state with { Error = null },
                PlannerOperation.EditExternal,
                view.SelectedAssignment.Identity,
                view.SelectedAssignment.ProjectPath),
            PlannerAction.ToggleCompleted when view.SelectedAssignment is not null => new PlannerTransition(
                state with { Error = null },
                PlannerOperation.ToggleCompleted,
                view.SelectedAssignment.Identity,
                view.SelectedAssignment.ProjectPath),
            PlannerAction.Edit or PlannerAction.EditExternal or
                PlannerAction.ToggleCompleted => Transition(state with
                {
                    Error = SelectionError(view)
                }),
            PlannerAction.Unschedule when view.SelectedSlot.Assignments.Length == 1 =>
                new PlannerTransition(
                    state with { Error = null },
                    PlannerOperation.Unschedule,
                    view.SelectedSlot.Assignments[0].Identity),
            PlannerAction.Unschedule => Transition(state with
            {
                Error = view.SelectedSlot.Assignments.Length > 1
                    ? "Resolve this conflicting timeslot before unscheduling."
                    : "No todo is assigned to this timeslot."
            }),
            PlannerAction.AssignOrMove when view.SelectedSlot.Assignments.Length > 1 => Transition(state with
            {
                Error = "This timeslot contains conflicting assignments."
            }),
            PlannerAction.AssignOrMove when view.SelectedSlot.Assignments.Length == 0 => Transition(state with
            {
                Mode = PlannerMode.ChooseTodo,
                PickerIndex = 0,
                Error = null
            }),
            PlannerAction.AssignOrMove => Transition(state with
            {
                Mode = PlannerMode.MoveTodo,
                MovingTodo = view.SelectedSlot.Assignments[0].Identity,
                Error = null
            }),
            _ => Transition(state)
        };

    public PlannerTransition Reduce(
        PlannerState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        PlannerView view)
    {
        if (state.Editor is not null)
        {
            return ApplyEditorTransition(
                state,
                todoEditorReducer.Reduce(
                    state.Editor,
                    key,
                    bindings,
                    view.Projects
                        .Select(project => new TodoEditorProjectOption(project.Title, project.Path))
                        .ToArray()));
        }

        if (state.Mode == PlannerMode.EditFilter)
        {
            return ReduceFilter(state, key);
        }

        if (bindings.MatchesBack(key) && state.Mode != PlannerMode.Browse)
        {
            return Transition(state with { Mode = PlannerMode.Browse, MovingTodo = null, Error = null });
        }

        if (state.Mode == PlannerMode.ChooseTodo)
        {
            if (bindings.MatchesFilterMode(key))
            {
                return Transition(state with
                {
                    Mode = PlannerMode.EditFilter,
                    FilterDraft = state.FilterText,
                    Error = null
                });
            }

            if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
            {
                var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
                return Transition(state with
                {
                    PickerIndex = MoveIndex(state.PickerIndex, offset, view.PickerTodos.Length),
                    Error = null
                });
            }

            if (bindings.MatchesOpen(key) && view.SelectedPickerTodo is not null)
            {
                return new PlannerTransition(
                    state with { Mode = PlannerMode.Browse, Error = null },
                    PlannerOperation.Schedule,
                    view.SelectedPickerTodo.Identity);
            }

            return Transition(state);
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            return Transition(state with
            {
                SlotIndex = MoveIndex(state.SlotIndex, offset, DayPlannerPresenter.SlotCount),
                Error = null
            });
        }

        if (bindings.MatchesPlannerPreviousDay(key) || bindings.MatchesPlannerNextDay(key))
        {
            var offset = bindings.MatchesPlannerPreviousDay(key) ? -1 : 1;
            return Transition(state with { SelectedDate = state.SelectedDate.AddDays(offset), Error = null });
        }

        if (bindings.MatchesPlannerToday(key))
        {
            return Transition(state with { SelectedDate = todayProvider(), Error = null });
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesToggleDetails(key))
        {
            return Transition(state with { ShowDetails = !state.ShowDetails, Error = null });
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesEditTodoExternal(key))
        {
            return view.SelectedAssignment is null
                ? Transition(state with { Error = SelectionError(view) })
                : new PlannerTransition(
                    state with { Error = null },
                    PlannerOperation.EditExternal,
                    view.SelectedAssignment.Identity,
                    view.SelectedAssignment.ProjectPath);
        }

        if (state.Mode == PlannerMode.Browse &&
            (bindings.MatchesEditTodo(key) || bindings.MatchesEditTodoContent(key)))
        {
            return view.SelectedAssignment is null
                ? Transition(state with { Error = SelectionError(view) })
                : Transition(state with
                {
                    Editor = todoEditorReducer.EditEditor(
                        view.SelectedAssignment.Todo,
                        view.SelectedAssignment.Identity),
                    Error = null
                });
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesToggleTodo(key))
        {
            return view.SelectedAssignment is null
                ? Transition(state with { Error = SelectionError(view) })
                : new PlannerTransition(
                    state with { Error = null },
                    PlannerOperation.ToggleCompleted,
                    view.SelectedAssignment.Identity,
                    view.SelectedAssignment.ProjectPath);
        }

        if (state.Mode == PlannerMode.MoveTodo && bindings.MatchesOpen(key))
        {
            if (view.SelectedSlot.Assignments.Length > 0)
            {
                return Transition(state with { Error = "That timeslot is already occupied." });
            }

            return new PlannerTransition(
                state with { Mode = PlannerMode.Browse, Error = null },
                PlannerOperation.Schedule,
                state.MovingTodo);
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesPlannerUnschedule(key))
        {
            if (view.SelectedSlot.Assignments.Length != 1)
            {
                return Transition(state with
                {
                    Error = view.SelectedSlot.Assignments.Length > 1
                        ? "Resolve this conflicting timeslot before unscheduling."
                        : "No todo is assigned to this timeslot."
                });
            }

            return new PlannerTransition(
                state with { Error = null },
                PlannerOperation.Unschedule,
                view.SelectedSlot.Assignments[0].Identity);
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesCreateTodo(key))
        {
            return view.SelectedSlot.Assignments.Length > 0
                ? Transition(state with { Error = "That timeslot is already occupied." })
                : view.Projects.Length == 0
                ? Transition(state with { Error = "No valid projects are available." })
                : Transition(state with
                {
                    Editor = todoEditorReducer.CreateEditor(null, true, SelectedSchedule(state), true),
                    Error = null
                });
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesOpen(key))
        {
            if (view.SelectedSlot.Assignments.Length > 1)
            {
                return Transition(state with { Error = "This timeslot contains conflicting assignments." });
            }

            return view.SelectedSlot.Assignments.Length == 0
                ? Transition(state with { Mode = PlannerMode.ChooseTodo, PickerIndex = 0, Error = null })
                : Transition(state with
                {
                    Mode = PlannerMode.MoveTodo,
                    MovingTodo = view.SelectedSlot.Assignments[0].Identity,
                    Error = null
                });
        }

        return Transition(state);
    }

    private static PlannerTransition ReduceFilter(PlannerState state, ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return Transition(state with
            {
                Mode = PlannerMode.ChooseTodo,
                FilterDraft = state.FilterText,
                Error = null
            });
        }

        if (key.Key == ConsoleKey.Enter)
        {
            var filter = state.FilterDraft.Trim();
            return Transition(state with
            {
                Mode = PlannerMode.ChooseTodo,
                FilterText = filter,
                FilterDraft = filter,
                PickerIndex = 0,
                Error = null
            });
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            return Transition(state with
            {
                FilterDraft = state.FilterDraft.Length == 0 ? string.Empty : state.FilterDraft[..^1],
                PickerIndex = 0
            });
        }

        return char.IsControl(key.KeyChar)
            ? Transition(state)
            : Transition(state with { FilterDraft = state.FilterDraft + key.KeyChar, PickerIndex = 0 });
    }

    private static int MoveIndex(int current, int offset, int count) =>
        count == 0 ? 0 : Math.Clamp(current + offset, 0, count - 1);

    private static TodoSchedule SelectedSchedule(PlannerState state) => new(
        state.SelectedDate,
        new TimeOnly(6, 0).AddMinutes(state.SlotIndex * 30));

    private static PlannerTransition ApplyEditorTransition(
        PlannerState state,
        TodoEditorTransition transition) => new(
            state with
            {
                Editor = transition.Operation == TodoEditorOperation.None ? transition.State : state.Editor,
                Error = null
            },
            transition.Operation switch
            {
                TodoEditorOperation.Create => PlannerOperation.Create,
                TodoEditorOperation.Update => PlannerOperation.Update,
                _ => PlannerOperation.None
            },
            transition.Target,
            transition.ProjectPath,
            transition.Update);

    private static string SelectionError(PlannerView view) =>
        view.SelectedSlot.Assignments.Length > 1
            ? "Resolve this conflicting timeslot before editing."
            : "No todo is assigned to this timeslot.";

    private static PlannerTransition Transition(PlannerState state) =>
        new(state, PlannerOperation.None, null);
}
