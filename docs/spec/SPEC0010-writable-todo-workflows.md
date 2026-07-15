# SPEC 0010: Writable Todo Workflows

## Status

Accepted

## Purpose

Extend the browser and planner with safe creation, editing, and completion of
Markdown todos.

## Behavior

- `a` creates a todo. An individual browser project is preselected; All and
  Planner require choosing a valid project.
- New todos are appended under `## Inbox`. Planner creation also applies the
  selected schedule.
- `e` edits title, external reference, priority, tags, start date, and due
  date. Project, section, schedule, notes, and subtasks remain unchanged.
- Spacebar toggles the selected Markdown checkbox. `:completed` continues to
  control completed-todo visibility only.
- The bottom form uses configured movement and open/back gestures. Ctrl+S
  saves; cancellation performs no write.
- Successful changes reload the catalog and restore selection to the resulting
  source identity. Validation, stale targets, and I/O failures remain visible
  without discarding external content.

Deletion, project/section movement, and note or subtask editing remain out of
scope.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0002: Project Browser](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
