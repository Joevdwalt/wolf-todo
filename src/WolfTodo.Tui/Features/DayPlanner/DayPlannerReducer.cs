using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayPlannerReducer(Func<DateOnly>? todayProvider = null)
{
    private readonly Func<DateOnly> todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
    private readonly TodoEditorReducer todoEditorReducer = new(todayProvider);

    public PlannerTransition ReduceAction(
        PlannerState state,
        PlannerAction action,
        PlannerView view,
        TimeSpan? defaultDuration = null) =>
        action switch
        {
            PlannerAction.PreviousDay => Transition(WithVisibleDate(state, state.SelectedDate.AddDays(-1))),
            PlannerAction.NextDay => Transition(WithVisibleDate(state, state.SelectedDate.AddDays(1))),
            PlannerAction.Today => Transition(WithVisibleDate(state, todayProvider())),
            PlannerAction.Create when view.Projects.Length > 0 => Transition(state with
            {
                Editor = todoEditorReducer.CreateEditor(
                    null,
                    true,
                    SelectedSchedule(state),
                    ScheduleRequirement(state),
                    defaultDuration),
                Error = null
            }),
            PlannerAction.Create => Transition(state with { Error = "No valid projects are available." }),
            PlannerAction.ToggleDetails => Transition(state with
            {
                ShowDetails = !state.ShowDetails,
                Error = null
            }),
            PlannerAction.Edit when view.SelectedFocusedAssignment is not null =>
                Transition(state with
            {
                Editor = todoEditorReducer.EditEditor(
                    view.SelectedFocusedAssignment.Todo,
                    view.SelectedFocusedAssignment.Identity),
                Error = null
            }),
            PlannerAction.EditExternal when view.SelectedFocusedAssignment is not null => new PlannerTransition(
                state with { Error = null },
                PlannerOperation.EditExternal,
                view.SelectedFocusedAssignment.Identity,
                view.SelectedFocusedAssignment.ProjectPath),
            PlannerAction.ToggleCompleted when view.SelectedFocusedAssignment is not null => new PlannerTransition(
                state with { Error = null },
                PlannerOperation.ToggleCompleted,
                view.SelectedFocusedAssignment.Identity,
                view.SelectedFocusedAssignment.ProjectPath),
            PlannerAction.Edit or PlannerAction.EditExternal or
                PlannerAction.ToggleCompleted => Transition(state with
                {
                    Error = SelectionError(view)
                }),
            PlannerAction.Unschedule when view.SelectedFocusedAssignment is not null =>
                new PlannerTransition(
                    state with { Error = null },
                    PlannerOperation.Unschedule,
                    view.SelectedFocusedAssignment.Identity),
            PlannerAction.Unschedule => Transition(state with
            {
                Error = SelectionError(view)
            }),
            PlannerAction.AssignOrMove when IsReadOnlyAllDaySelection(view) => Transition(state with
            {
                Error = "Calendar all-day items are read-only."
            }),
            PlannerAction.AssignOrMove when view.SelectedFocusedAssignment is null => Transition(state with
            {
                Mode = PlannerMode.ChooseTodo,
                PickerIndex = 0,
                Error = null
            }),
            PlannerAction.AssignOrMove => Transition(state with
            {
                Mode = PlannerMode.MoveTodo,
                MovingTodo = view.SelectedFocusedAssignment.Identity,
                Error = null
            }),
            _ => Transition(state)
        };

    public PlannerTransition Reduce(
        PlannerState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        PlannerView view,
        TimeSpan? defaultDuration = null)
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

        if (state.Mode == PlannerMode.Browse && bindings.MatchesPlannerToggleView(key))
        {
            var enteringMultiDay = state.ViewMode == PlannerViewMode.SingleDay;
            return Transition(state with
            {
                ViewMode = enteringMultiDay ? PlannerViewMode.MultiDay : PlannerViewMode.SingleDay,
                VisibleStartDate = enteringMultiDay ? state.SelectedDate : null,
                Error = null
            });
        }

        if (state.Mode is PlannerMode.Browse or PlannerMode.MoveTodo &&
            state.ViewMode == PlannerViewMode.MultiDay)
        {
            if (state.Mode == PlannerMode.Browse &&
                (bindings.MatchesPlannerIncreaseRange(key) || bindings.MatchesPlannerDecreaseRange(key)))
            {
                var delta = bindings.MatchesPlannerIncreaseRange(key) ? 1 : -1;
                return Transition(state with { VisibleDayCount = Math.Clamp(state.VisibleDayCount + delta, 1, 3), Error = null });
            }

            var movesPreviousColumn = bindings.MatchesPlannerPreviousColumn(key);
            var movesNextColumn = bindings.MatchesPlannerNextColumn(key);
            if (movesPreviousColumn || movesNextColumn)
            {
                var delta = movesPreviousColumn ? -1 : 1;
                return Transition(WithVisibleDate(state, state.SelectedDate.AddDays(delta)));
            }
        }

        if ((state.Mode == PlannerMode.MoveTodo &&
             (key.Key == ConsoleKey.Escape ||
              (state.ViewMode != PlannerViewMode.MultiDay && bindings.MatchesBack(key)))) ||
            (state.Mode != PlannerMode.Browse && state.Mode != PlannerMode.MoveTodo && bindings.MatchesBack(key)))
        {
            return Transition(state with { Mode = PlannerMode.Browse, MovingTodo = null, Error = null });
        }

        if (state.Mode is PlannerMode.Browse or PlannerMode.MoveTodo &&
            (bindings.MatchesFocusNext(key) || bindings.MatchesFocusPrevious(key)))
        {
            return Transition(state with
            {
                Focus = state.Focus == PlannerFocus.Timeline ? PlannerFocus.AllDay : PlannerFocus.Timeline,
                Error = null
            });
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
                    view.SelectedPickerTodo.Identity,
                    ScheduleTarget: ScheduleTarget(state));
            }

            return Transition(state);
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesFilterMode(key))
        {
            return Transition(state with
                {
                    Mode = PlannerMode.EditFilter,
                    FilterDraft = state.FilterText,
                    PickerIndex = 0,
                    Error = null
                });
        }

        if (bindings.MatchesJumpTop(key) || bindings.MatchesJumpBottom(key))
        {
            return Transition(state with
            {
                SlotIndex = state.Focus == PlannerFocus.Timeline
                    ? bindings.MatchesJumpTop(key) ? 0 : DayPlannerPresenter.SlotCount - 1
                    : state.SlotIndex,
                AllDayIndex = state.Focus == PlannerFocus.AllDay
                    ? bindings.MatchesJumpTop(key) ? 0 : Math.Max(0, view.CalendarAgenda.AllDayItems.Length - 1)
                    : state.AllDayIndex,
                Error = null
            });
        }

        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            if (state.Mode == PlannerMode.Browse &&
                state.Focus == PlannerFocus.Timeline &&
                IsStackSelectionKey(key) &&
                view.SelectedSlot.Items.Length > 1)
            {
                var selectedIndex = Array.FindIndex(
                    view.SelectedSlot.Items.ToArray(),
                    item => item.Identity == view.SelectedItem?.Identity);
                var nextIndex = selectedIndex + offset;
                if (nextIndex >= 0 && nextIndex < view.SelectedSlot.Items.Length)
                {
                    return Transition(state with
                    {
                        SelectedTimelineItemIdentity = view.SelectedSlot.Items[nextIndex].Identity,
                        Error = null
                    });
                }

                return Transition(state with
                {
                    SlotIndex = MoveIndex(state.SlotIndex, offset, DayPlannerPresenter.SlotCount),
                    SelectedTimelineItemIdentity = null,
                    Error = null
                });
            }

            return Transition(state with
            {
                SlotIndex = state.Focus == PlannerFocus.Timeline
                    ? MoveIndex(state.SlotIndex, offset, DayPlannerPresenter.SlotCount)
                    : state.SlotIndex,
                AllDayIndex = state.Focus == PlannerFocus.AllDay
                    ? MoveIndex(state.AllDayIndex, offset, view.CalendarAgenda.AllDayItems.Length)
                    : state.AllDayIndex,
                Error = null
            });
        }

        if (bindings.MatchesPlannerPreviousDay(key) || bindings.MatchesPlannerNextDay(key))
        {
            var offset = bindings.MatchesPlannerPreviousDay(key) ? -1 : 1;
            return Transition(WithVisibleDate(state, state.SelectedDate.AddDays(offset)));
        }

        if (bindings.MatchesPlannerToday(key))
        {
            return Transition(WithVisibleDate(state, todayProvider()));
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesToggleDetails(key))
        {
            return Transition(state with { ShowDetails = !state.ShowDetails, Error = null });
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesEditTodoExternal(key))
        {
            return view.SelectedFocusedAssignment is null
                ? Transition(state with { Error = SelectionError(view) })
                : new PlannerTransition(
                    state with { Error = null },
                    PlannerOperation.EditExternal,
                    view.SelectedFocusedAssignment.Identity,
                    view.SelectedFocusedAssignment.ProjectPath);
        }

        if (state.Mode == PlannerMode.Browse &&
            (bindings.MatchesEditTodo(key) || bindings.MatchesEditTodoContent(key)))
        {
            return view.SelectedFocusedAssignment is null
                ? Transition(state with { Error = SelectionError(view) })
                : Transition(state with
                {
                    Editor = todoEditorReducer.EditEditor(
                        view.SelectedFocusedAssignment.Todo,
                        view.SelectedFocusedAssignment.Identity),
                    Error = null
                });
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesToggleTodo(key))
        {
            return view.SelectedFocusedAssignment is null
                ? Transition(state with { Error = SelectionError(view) })
                : new PlannerTransition(
                    state with { Error = null },
                    PlannerOperation.ToggleCompleted,
                    view.SelectedFocusedAssignment.Identity,
                    view.SelectedFocusedAssignment.ProjectPath);
        }

        if (state.Mode == PlannerMode.MoveTodo && bindings.MatchesOpen(key))
        {
            return new PlannerTransition(
                state with { Mode = PlannerMode.Browse, Error = null },
                PlannerOperation.Schedule,
                state.MovingTodo,
                ScheduleTarget: ScheduleTarget(state));
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesPlannerUnschedule(key))
        {
            if (view.SelectedFocusedAssignment is null)
            {
                return Transition(state with
                {
                    Error = SelectionError(view)
                });
            }

            return new PlannerTransition(
                state with { Error = null },
                PlannerOperation.Unschedule,
                view.SelectedFocusedAssignment.Identity);
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesCreateTodo(key))
        {
            return view.Projects.Length == 0
                ? Transition(state with { Error = "No valid projects are available." })
                : Transition(state with
                {
                    Editor = todoEditorReducer.CreateEditor(
                        null,
                        true,
                        SelectedSchedule(state),
                        ScheduleRequirement(state),
                        defaultDuration),
                    Error = null
                });
        }

        if (state.Mode == PlannerMode.Browse && bindings.MatchesOpen(key))
        {
            if (IsReadOnlyAllDaySelection(view))
            {
                return Transition(state with { Error = "Calendar all-day items are read-only." });
            }

            return view.SelectedFocusedAssignment is null
                ? Transition(state with { Mode = PlannerMode.ChooseTodo, PickerIndex = 0, Error = null })
                : Transition(state with
                {
                    Mode = PlannerMode.MoveTodo,
                    MovingTodo = view.SelectedFocusedAssignment.Identity,
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

    private static PlannerState WithVisibleDate(PlannerState state, DateOnly selectedDate)
    {
        state = state.SaveActivePane();
        if (state.ViewMode != PlannerViewMode.MultiDay)
        {
            return state with { SelectedDate = selectedDate, Error = null };
        }

        var visibleStart = state.VisibleStartDate ?? state.SelectedDate;
        var visibleEnd = visibleStart.AddDays(state.VisibleDayCount - 1);
        if (selectedDate < visibleStart)
        {
            visibleStart = selectedDate;
        }
        else if (selectedDate > visibleEnd)
        {
            visibleStart = selectedDate.AddDays(1 - state.VisibleDayCount);
        }

        state.PaneCursors.TryGetValue(selectedDate, out var destinationCursor);
        return state with
        {
            SelectedDate = selectedDate,
            // Timeline position is shared across date panes. A pane-specific
            // item can only be restored when it belongs to that same slot.
            SlotIndex = state.SlotIndex,
            SelectedTimelineItemIdentity = destinationCursor?.SlotIndex == state.SlotIndex
                ? destinationCursor.SelectedTimelineItemIdentity
                : null,
            AllDayIndex = destinationCursor?.AllDayIndex ?? 0,
            VisibleStartDate = visibleStart,
            Error = null
        };
    }

    private static bool IsStackSelectionKey(ConsoleKeyInfo key) =>
        key.KeyChar is 'j' or 'J' or 'k' or 'K';

    private static TodoSchedule SelectedSchedule(PlannerState state) =>
        state.Focus == PlannerFocus.AllDay
            ? new TodoSchedule(state.SelectedDate)
            : new TodoSchedule(
                state.SelectedDate,
                new TimeOnly(6, 0).AddMinutes(state.SlotIndex * 15));

    private static TodoScheduleRequirement ScheduleRequirement(PlannerState state) =>
        state.Focus == PlannerFocus.AllDay
            ? TodoScheduleRequirement.Date
            : TodoScheduleRequirement.DateAndTime;

    private static PlannerScheduleTarget ScheduleTarget(PlannerState state) =>
        state.Focus == PlannerFocus.AllDay ? PlannerScheduleTarget.AllDay : PlannerScheduleTarget.Timeline;

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
        IsReadOnlyAllDaySelection(view)
            ? "Calendar all-day items are read-only."
            : view.State.Focus == PlannerFocus.AllDay
                ? "No todo is selected in All Day."
            : "No todo is assigned to this timeslot.";

    private static bool IsReadOnlyAllDaySelection(PlannerView view) =>
        view.State.Focus == PlannerFocus.AllDay &&
        view.SelectedAllDayItem is not null &&
        view.SelectedAllDayAssignment is null;

    private static PlannerTransition Transition(PlannerState state) =>
        new(state.SaveActivePane(), PlannerOperation.None, null);
}
