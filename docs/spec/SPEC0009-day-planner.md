# SPEC 0009: Day Planner

## Status

Accepted

## Purpose

Add a second application tab that assigns Markdown todos to a dated quarter-hour
agenda without introducing separate planner storage.

## Behavior

Show 64 slots from 06:00 through 21:45 for the selected date. Render the `:00`
and `:15` task slots stacked under each 30-minute time label, so the time column
shows only `:00` and `:30` labels. Start on today
each launch and preserve planner state while switching tabs. Keep the selected
slot visible on short terminals and show scheduled completed todos dimly.

Show a single all-day strip above the timeline when it has content. It contains
date-only Wolf Todo schedules and, when configured, Google Calendar all-day
events, focus time, and out-of-office entries. Date-only todos are editable
through the normal task editor but are not slot assignments.

An optional, read-only Google Calendar primary-calendar overlay may display
timed meetings in their overlapping slots. It uses desktop OAuth configured by
an absolute client JSON path, stores refresh credentials in application state,
and refreshes with the configured `r` binding. Meetings produce overlap
warnings only; they never reserve slots or block todo assignment. The planner
shows syncing, sign-in, configuration, and offline states without making the
planner unusable.

On today, add a logical current-time row immediately before the next quarter-hour
slot. Show the exact `HH:mm` value in the time column and fill the plan column
with `▶` followed by `─` characters in the configured bright accent. Fill the
actual rendered cell width without wrapping. This row is timeline content: do
not use panel borders, active-row backgrounds, intersections, or surface fills. Before 06:00 place it before the first slot; after
21:45 place it after the last. Keep the selected slot visible when it cannot fit
with the marker. While Planner is active, an idle one-minute input timeout
redraws without changing application state so the line remains current.

Enter or `l` on an empty slot opens a filterable picker of all open,
unscheduled todos from valid projects. Show several candidates at once, keep
the selection visible while scrolling, and update the list while filter input
changes. The same action on an occupied slot starts move mode. `u` unschedules,
`[`/`]` change dates, and `g` returns to today. Esc or `h` cancels modal work.
Occupied destinations are never replaced.

Show details for the selected assignment by default. Wide terminals place an
`INSPECTOR` beside the timeline; narrower terminals show a compact `SELECTED`
summary beneath it. Timeline assignments show compact state and priority before
their title. `v` hides or restores details for the current session. Conflicting slots
show a diagnostic instead of exposing an ambiguous todo.

On an occupied slot, `e` edits fields including scheduled date and time, `E` edits notes and subtasks, Ctrl+E
opens the Markdown source in `$EDITOR`, and Space toggles completion without
removing the schedule. `a` on an empty slot uses the same complete field form
as the Todos tab, pre-fills the selected date and time, and requires both values.
After rescheduling through the field editor, Planner follows the todo to its new
date and slot. Clearing both schedule fields on an existing todo unschedules it.

All actions use configured bindings. Picker, move, and create input capture
keystrokes before application-tab switching. Conflicting assignments are
rendered as diagnostics and block further assignment to that slot.

When no planner modal is active, the configured command launcher opens global
command mode. Quit, completed visibility, cancellation, and unknown-command
feedback match the Todos view.

## Acceptance Scenarios

1. The tab shell switches between Todos and Day Planner without losing either
   feature's in-process state.
2. Assignment, movement, unscheduling, filtering, and direct creation update
   the source Markdown and reload the catalog.
3. Completed assignments remain visible but cannot be selected as unscheduled
   work.
4. Wide, narrow, and short terminals retain access to slots and status hints.
5. Wrapped status hints reduce the visible slot window instead of scrolling the
   tab strip off the top of the terminal.
6. Full and compact details, multi-row picking, and editor forms remain usable
   without exceeding the terminal viewport.
7. Planner property, content, completion, and external-editor actions use the
   same conflict-safe Markdown workflows as the Todos tab.
8. Today's current-time row uses the highlight foreground without resembling a
   table border, stays within the viewport budget, and advances while idle.
9. Date-only schedules and calendar all-day items fit in the all-day header
   without reducing access to the selected timeline slot.
10. Calendar overlap warnings and failed calendar refreshes never block normal
    Markdown todo scheduling.

## References

- [SPEC0005: Application View Tabs](SPEC0005-application-view-tabs.md)
- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
