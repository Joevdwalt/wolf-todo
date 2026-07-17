# SPEC 0009: Day Planner

## Status

Accepted

## Purpose

Add a second application tab that assigns Markdown todos to a dated half-hour
agenda without introducing separate planner storage.

## Behavior

Show 32 slots from 06:00 through 21:30 for the selected date. Start on today
each launch and preserve planner state while switching tabs. Keep the selected
slot visible on short terminals and show scheduled completed todos dimly.

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

## References

- [SPEC0005: Application View Tabs](SPEC0005-application-view-tabs.md)
- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
