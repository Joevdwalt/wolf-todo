# SPEC 0010: Writable Todo Workflows

## Status

Accepted

## Purpose

Extend the browser and planner with safe creation, editing, and completion of
Markdown todos.

## Behavior

- `a` creates a todo. An individual browser project is preselected; All and
  Planner require choosing a valid project. Planner uses the same complete
  task editor with its selected schedule pre-filled and required.
- New todos are appended under `## Inbox`. Planner creation also applies the
  selected schedule.
- `e` and its compatibility alias `E` open one editor for title, external
  reference, priority, tags, scheduled date/time, direct notes, and direct
  subtasks. Project, section, and compatibility-only start/due metadata remain
  unchanged. The command palette exposes one Edit todo action.
- Spacebar toggles the selected Markdown checkbox. `:completed` continues to
  control completed-todo visibility only.
- The bottom editor uses configured movement and open/back gestures and one
  cursor across six compact field rows and the ordered content outline. Ctrl+S
  saves; cancellation performs no write. A field row places label and value on
  one physical line:

  ```text
  > TITLE            Renew contract
    REFERENCE        EXT-42
    PRIORITY         —
    CONTENT
    • Review current contract
  ```

  Use `—` for empty committed values and `_` as the text-entry cursor. A
  viewport keeps the selected row visible on shorter terminals, truncating
  long values with an ellipsis. Explicitly wrap hints and validation errors so
  the status panel remains within the terminal viewport. Use the configured theme hierarchy:
  bold headings for labels, ordinary text for inactive values, bold accent for
  the selected value, dim muted styling for placeholders and hints, and bold
  error styling for validation failures.
- Successful changes reload the catalog and restore selection to the resulting
  source identity. Validation, stale targets, and I/O failures remain visible
  without discarding external content or the active form. Schedule writes also
  reject slots occupied by another configured todo.
- Within the editor, `a` opens a configured-binding picker for a note or
  subtask. It inserts after selected content or appends when a field is selected.
  Add, edit, remove, field, schedule, and subtask completion changes are written
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
- The unified task editor is shared by Todos and Day Planner. Planner write
  failures keep the active draft open with its error;
  successful writes close the editor and retain the selected date and slot.

Root-todo deletion, project/section movement, and content reordering remain out
of scope.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [ADR0010: Edit Markdown with the Configured External Editor](../adr/ADR0010-edit-markdown-with-the-configured-external-editor.md)
- [SPEC0002: Project Browser](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
