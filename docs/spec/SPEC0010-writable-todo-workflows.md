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
- `e` and its compatibility alias `E` open the unified task editor described in
  [SPEC0011](SPEC0011-structured-todo-content-editor.md) for title, external
  reference, priority, tags, scheduled date/time, Content, and subtasks.
  Project, section, and compatibility-only start/due metadata remain
  unchanged. The command palette exposes one Edit todo action.
- Spacebar toggles the selected Markdown checkbox. `:completed` continues to
  control completed-todo visibility only.
- The unified editor uses configured movement and open/back gestures. Ctrl+S
  saves the draft and cancellation performs no write. Its presentation,
  multiline Content behavior, subtask-only Add action, validation, and removal
  rules are defined by SPEC0011.
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
- `:archive` moves every completed top-level todo whose descendants are also
  completed from the selected concrete project into its companion archive file.
  For `work.md`, use `work.archive.md` in the same directory. Create it with
  `# <project> Archive` and `## Archived` when absent, then append later task
  blocks. Keep completed subtasks beneath an open parent in the source project.
  Write the archive first, then rewrite the source; if source removal fails,
  retain the archive copy and report the duplicate-safe failure. Archive files
  are not configured projects and `:archive` is unavailable from aggregate,
  saved, or Planner views.
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

Root-todo deletion, project/section movement, and subtask reordering remain out
of scope.

Multi-task schedule, tag, priority, and completion changes use the selection,
form, and per-project atomicity rules in SPEC0018.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [ADR0010: Edit Markdown with the Configured External Editor](../adr/ADR0010-edit-markdown-with-the-configured-external-editor.md)
- [SPEC0002: Project Browser](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
- [SPEC0011: Structured Todo Content Editor](SPEC0011-structured-todo-content-editor.md)
- [SPEC0018: Multi-Select Task Updates](SPEC0018-multi-select-task-updates.md)
