# SPEC 0018: Multi-Select Task Updates

## Status

Accepted

## Purpose

Allow one transient selection of Markdown-backed tasks to receive a shared
scheduled date, tag change, priority, or completion update from the Todos view.

## Behavior

- The configured `toggle_todo_selection` gesture, `m` by default, marks or
  unmarks the task under the Todos cursor. The configured `bulk_edit_todos`
  gesture, `b` by default, opens the bulk editor. The configured
  `clear_todo_selection` gesture, `Ctrl+M` by default, clears every mark.
- Marks work in concrete projects, All, `@today`, saved-query views, and nested
  subtasks. Headings and project errors cannot be marked. Marks survive browser
  navigation, project-view changes, sorting, filtering, and a cancelled or
  rejected bulk edit. They are not persisted and clear when leaving Todos,
  after their successful update, or after another successful write that may
  invalidate source-line identities.
- The cursor and marks are independent. Task rows show a dedicated mark column,
  keep cursor styling distinct, and apply the mark surface to both title and tag
  lines. Status hints include the total mark count even when some marks are not
  visible in the current view.
- The bulk editor applies one draft to all marked tasks. Its controls are:
  - Scheduled date: unchanged, set, or clear. Set accepts the task editor's
    absolute and relative date expressions, preserves each existing scheduled
    time, and makes an unscheduled task all-day. Clear removes date and time.
  - Tags: unchanged, add, remove, or replace. Tags are parsed from comma- or
    whitespace-separated text, stripped of leading `#`, and deduplicated
    case-insensitively. Add appends new tags, remove preserves retained order,
    and replace uses the entered order and permits an empty set. The form uses
    a leading `+`, `-`, or `=` to choose add, remove, or replace; an empty value
    leaves tags unchanged and `=` alone replaces them with an empty set.
  - Priority: unchanged, clear, or Lowest, Low, Medium, High, or Highest.
  - Completion: unchanged or complete. Complete is idempotent and never reopens
    an already completed task.
- Configured move and open gestures navigate and edit the bulk form. Ctrl+S (or
  the configured save gesture) applies it; Escape (or the configured back
  gesture) cancels without clearing marks. At least one change is required.
- Setting a date for timed tasks preserves each task's time and allows tasks to
  share a timeslot. Bulk updates do not run a schedule-conflict preflight.
- Marked identities are grouped by Markdown project. Each project is re-read;
  every selected target in that file is validated against its parsed snapshot;
  and all task-line changes are applied through one atomic file replacement.
  A stale target or write failure leaves that project unchanged. Other project
  groups continue, successful marks clear, failed marks remain, and the status
  reports updated and failed task and project counts.
- Existing single-task edit, external edit, Spacebar completion, timers, and
  Planner behavior continue to target only the cursor task.

## Acceptance Scenarios

1. Mark tasks across two projects and apply one priority; both files update and
   the marks clear.
2. Add, remove, replace, and clear tags without changing unrelated task fields
   or Markdown content.
3. Set one date across timed and unscheduled tasks while preserving existing
   times; clear removes complete schedules.
4. Completing a mixture of open and completed tasks leaves all selected tasks
   completed.
5. A stale task rejects its complete project group without writing that file,
   while valid project groups succeed and only failed marks remain.
6. Setting the same date for overlapping timed tasks succeeds and preserves
   each task's time and duration.
7. Sorting, filtering, and switching browser views retain marks and show their
   total count; leaving Todos clears them.
8. Narrow and wide layouts distinguish cursor, marked, completed, and tagged
   rows without separating a task's title and tag lines.
9. Cancelling the form or submitting no changes writes nothing and retains the
   marked tasks.

## References

- [SPEC0002: Project Browser](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
