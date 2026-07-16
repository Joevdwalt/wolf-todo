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
- Uppercase `E` opens a structured draft for the selected todo's direct notes
  and subtasks. Add, edit, remove, and subtask completion changes are written
  together with Ctrl+S; Escape discards them.
- Removing a subtask includes its descendant subtree and requires confirmation
  when nested content exists.
- Ctrl+E opens the selected todo's canonical Markdown project at its one-based
  source line in the executable named by `$EDITOR`. Suspend terminal rendering,
  wait for the editor, then reset the terminal and reload the catalog.
- Use `path:line` for Helix, `+line path` for Vi/Vim/Neovim and Nano, and the
  plain path for unknown editors. Pass arguments without a shell. `$EDITOR`
  values containing embedded arguments are unsupported.
- Preserve project, filter, sort, and logical list position after reloading;
  clear stale source-line restoration. Missing editors, launch failures, and
  nonzero exits are recoverable browser errors.

Root-todo deletion, project/section movement, content reordering, and multiline
notes remain out of scope.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [ADR0010: Edit Markdown with the Configured External Editor](../adr/ADR0010-edit-markdown-with-the-configured-external-editor.md)
- [SPEC0002: Project Browser](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
