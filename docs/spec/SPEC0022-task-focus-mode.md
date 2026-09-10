# SPEC 0022: Task Focus Mode

## Status

Accepted

## Purpose

Provide a distraction-free workspace for one Markdown-backed todo while keeping
its normal editing, completion, movement, and timing workflows available.

## Behavior

The configurable `focus_task` binding, `f` by default, focuses the selected todo
from Todos or Day Planner. Calendar-only items and empty selections cannot open
focus mode. Focus is temporary: it does not start a timer, persist between
launches, or change Markdown by itself.

Focus mode replaces the tab strip, navigation, task list, planner, and normal
operational header with a centered task card. The card shows the project, root
task, metadata, direct notes, and the complete nested subtask tree. It remains
readable at narrow sizes and windows long content around the selected item.

The root and every nested subtask are selectable. Configured movement and jump
bindings move through them in source order. The configured completion binding
toggles the highlighted item. The existing edit bindings open the unified task
editor for it; the footer presents uppercase `E`. External editing remains
available. Escape closes a nested editor before a subsequent Escape exits focus
mode and restores the unchanged originating tab state.

Stopwatch and linked Pomodoro actions target the highlighted item. Entering
focus does not start either. The timer row appears only while a timer is active
and retains the existing single-timer, switching, completion, and logging rules.

Command mode and the command palette remain available. The focus palette
contains application commands plus edit, external edit, completion, timing, and
exit-focus actions. Tab, list, filter, sort, bulk, create, archive, project-wide,
and planner-navigation actions remain unavailable. `:move-todo-project` moves
the highlighted subtree and follows it as the new focused root.

Successful writes reload the catalog and retain focus using the resulting
source identity. Completing the root does not exit. If reload can no longer
resolve the root, close focus mode and report that the focused task is no longer
available in the originating view.

## Acceptance Scenarios

1. `f` focuses a selected browser or planner todo without starting a timer.
2. Focus rendering contains only the focus header, task card, active overlays,
   and contextual footer at wide, medium, narrow, and short sizes.
3. Movement selects nested subtasks; `E` edits and Space toggles the highlighted
   item while focus remains active.
4. Stopwatch and Pomodoro actions use the highlighted root or subtask and render
   a timer row only while active.
5. Project movement follows the moved subtree as the new focused root.
6. Escape returns to the originating tab and missing tasks exit safely with a
   recoverable message.
7. Custom `focus_task` gestures replace `f` and participate in conflict
   validation and command-palette hints.

## References

- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0011: Structured Todo Content Editor](SPEC0011-structured-todo-content-editor.md)
- [SPEC0012: Global Command Palette](SPEC0012-global-command-palette.md)
- [SPEC0016: Weekly Task Time Tracking](SPEC0016-weekly-task-time-tracking.md)
- [SPEC0017: Pomodoro Timer](SPEC0017-pomodoro-timer.md)
