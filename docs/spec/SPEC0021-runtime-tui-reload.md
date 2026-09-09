# SPEC 0021: Runtime TUI Reload

## Status

Draft

## Purpose

Keep a running TUI synchronized with external changes to the global
`config.toml` and the exact Markdown project files named by that configuration.
Markdown files remain the only durable todo store.

## Behavior

- Debounce filesystem events and redraw the current screen in place. Do not
  replay the splash screen or require keyboard input before reloading.
- A valid config change applies the new project list, keybindings, theme,
  sidebar, planner, calendar, export, and timer settings. Update the watched
  project set and reload the complete catalog once.
- A missing, unreadable, or invalid runtime config keeps the last valid config
  active and shows a recoverable error. Apply a later valid save normally.
- A configured project change reloads the complete catalog. Missing or invalid
  projects use the existing project-error presentation. Ignore unconfigured
  Markdown, archives, planner exports, and time logs.
- Preserve the active tab, sidebar selection when it still exists, visible todo
  position, filters, commands, planner state, and open drafts. Fall back to
  `All` when the selected sidebar item disappears.
- Drafts and marked todos retain their pre-reload snapshots. Saving after an
  incompatible external edit must fail conflict validation instead of updating
  a task that moved to the same source line.
- A timer or Pomodoro retains the timer settings captured when it started. New
  timer settings apply to the next timer. Refresh the calendar sync window on a
  valid config reload, retaining cached entries when calendar settings match.
- Show a short success notice until the next input. Keep config-reload errors
  visible until a valid configuration is loaded.

The CLI remains request-based and is outside this specification.

## References

- [ADR0004: Use a Global TOML Configuration](../adr/ADR0004-use-a-global-toml-configuration.md)
- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
