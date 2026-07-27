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
  cursor across seven field textboxes and the ordered content outline. Ctrl+S
  saves; cancellation performs no write. Every task field uses the same
  textbox presentation:

  ```text
  Title
  ╭──────────────────╮
  │Renew contract    │
  ╰──────────────────╯
  Reference
  ╭──────────────────╮
  │EXT-42            │
  ╰──────────────────╯
  Priority
  ╭──────────────────╮
  │—                 │
  ╰──────────────────╯
    CONTENT
    • Review current contract
  ```

  Textboxes are read-only while browsing and editable when opened. The
  Reference textbox contains the bare identifier; Markdown writes it as
  `(REFERENCE) ` before the task title. Empty values display as `—`. A field
  viewport keeps the selected textbox visible on shorter terminals; moving
  through the fields scrolls that viewport. Explicitly wrap hints and
  validation errors so the status panel remains within the terminal viewport.
  Use the configured theme hierarchy: white labels and active text, subdued
  white read-only text, dim muted styling for hints, and bold error styling for
  validation failures.
- Successful changes reload the catalog and restore selection to the resulting
  source identity. Validation, stale targets, and I/O failures remain visible
  without discarding external content or the active form. Schedule writes also
  reject slots occupied by another configured todo.
- `:roll-today`, the command-palette action, and the configured
  `roll_project_today` binding update every incomplete todo in the selected
  concrete project whose scheduled date is before the current local date.
  Include nested subtasks, preserve scheduled times and other metadata, and
  leave completed, unscheduled, current-day, and future todos unchanged.
  Revalidate the complete eligible set and apply it in one atomic project
  write; stale content aborts the rollover without partial changes.
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
