# ADR 0007: Persist TUI Session State Separately

## Status

Accepted

## Context

The project browser originally always opened on the virtual `All` project and
reset its sort to source order. Users need the TUI to resume the useful browser
context they chose without turning transient UI state into project Markdown
content or editable configuration.

Project list positions are unstable because projects are sorted and may be
added or removed. Canonical Markdown paths already provide stable runtime
identity for configured projects.

## Decision

Persist the selected project's canonical path and the active todo sort property
and direction in a small `state.json` file. Represent the virtual `All` project
with a null path. Resolve state files from platform-standard application-state
locations:

- Linux: `$XDG_STATE_HOME/wtodo/state.json`, falling back to
  `~/.local/state/wtodo/state.json`.
- macOS: `~/Library/Application Support/wtodo/state.json`.
- Windows: `%LOCALAPPDATA%\wtodo\state.json`.

Load session state after loading the configured project catalog and restore the
project by path rather than list index. Fall back to `All` when the file is
missing, malformed, inaccessible, or references a project that is no longer in
the catalog. Restore source sorting when sort state is absent or invalid without
discarding an otherwise valid project path. This keeps path-only files written
by older versions compatible. Save the current project and sort when the
interactive application exits, including when it exits from another tab.

Every launch starts on the Todos tab with focus in the Todos pane. Do not
persist the active tab, focused pane, selected todo, filter, completed
visibility, details visibility, modal state, or Day Planner state.

Treat session state as best-effort. A read or write failure must not prevent
startup or exit. Do not store todo content in this file and do not modify the
global configuration or project Markdown files.

## Consequences

- The browser resumes on the same project and sort across normal application
  restarts.
- Restoration remains correct when project ordering changes.
- Legacy session files remain readable, and an invalid sort independently falls
  back to source order.
- Launch behavior remains predictable by always opening Todos with its todo
  list focused.
- Session-state corruption is recoverable by falling back to the initial
  browser state.
- State write failures are intentionally silent and do not block application
  use.

## References

- [ADR0004: Use a Global TOML Configuration](ADR0004-use-a-global-toml-configuration.md)
- [SPEC0002: Project Browser and Markdown Todo Format](../spec/SPEC0002-project-browser-and-markdown-todo-format.md)
