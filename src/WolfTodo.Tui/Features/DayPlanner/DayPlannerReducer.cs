using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayPlannerReducer(Func<DateOnly>? todayProvider = null)
{
    private readonly Func<DateOnly> todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public PlannerTransition Reduce(
        PlannerState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        PlannerView view)
    {
        if (state.Mode == PlannerMode.EditFilter)
        {
            return ReduceFilter(state, key);
        }

        if (state.Mode == PlannerMode.ChooseCreateProject)
        {
            if (bindings.MatchesBack(key))
            {
                return Transition(state with { Mode = PlannerMode.Browse, Error = null });
            }

            if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
            {
                var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
                return Transition(state with
                {
                    CreateProjectIndex = MoveIndex(state.CreateProjectIndex, offset, view.Projects.Length),
                    Error = null
                });
            }

            if (bindings.MatchesOpen(key) && view.Projects.Length > 0)
            {
                return Transition(state with
                {
                    Mode = PlannerMode.EnterCreateTitle,
                    CreateProjectPath = view.Projects[state.CreateProjectIndex].Path,
                    CreateTitleDraft = string.Empty,
                    Error = null
                });
            }

            return Transition(state);
        }

        if (state.Mode == PlannerMode.EnterCreateTitle)
        {
            if (key.Key == ConsoleKey.Escape)
            {
                return Transition(state with { Mode = PlannerMode.Browse, Error = null });
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                return Transition(state with
                {
                    CreateTitleDraft = state.CreateTitleDraft.Length == 0
                        ? string.Empty
                        : state.CreateTitleDraft[..^1],
                    Error = null
                });
            }

            if (key.Key == ConsoleKey.Enter)
            {
                if (string.IsNullOrWhiteSpace(state.CreateTitleDraft))
                {
                    return Transition(state with { Error = "Title is required." });
                }

                return new PlannerTransition(
                    state with { Mode = PlannerMode.Browse, Error = null },
                    PlannerOperation.Create,
                    null,
                    state.CreateProjectPath,
                    new TodoUpdate(state.CreateTitleDraft.Trim(), null, null, [], null, null));
            }

            return char.IsControl(key.KeyChar)
                ? Transition(state)
                : Transition(state with
                {
                    CreateTitleDraft = state.CreateTitleDraft + key.KeyChar,
                    Error = null
                });
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
            return view.Projects.Length == 0
                ? Transition(state with { Error = "No valid projects are available." })
                : Transition(state with
                {
                    Mode = PlannerMode.ChooseCreateProject,
                    CreateProjectIndex = 0,
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

    private static PlannerTransition Transition(PlannerState state) =>
        new(state, PlannerOperation.None, null);
}
