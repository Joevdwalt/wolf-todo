# SPEC 0004: Configurable Browser Key Bindings

## Status

Accepted

## Purpose

Define configurable browser inputs and Vim-style navigation while preserving
the existing keyboard interaction and command syntax.

## Default Interaction

- Down Arrow or `j` moves to the next project or todo.
- Up Arrow or `k` moves to the previous project or todo.
- Enter or `l` opens or advances from projects to todos to details without
  wrapping at the final pane.
- Esc or `h` returns from details to todos to projects without wrapping at the
  first pane.
- Tab and Shift+Tab cycle focus forward and backward through visible panes.
- `:` enters command mode and `/` enters filter mode.
- `:completed` toggles completed todos and the required configured quit command
  exits the application.

These inputs are resolved from the global configuration described by ADR0005.
Configured arrays replace the defaults for their action. A custom command-mode
gesture still opens a colon-prefixed command prompt.

While command or filter mode is active, Enter submits, Esc cancels, and
Backspace edits. Printable navigation bindings are entered as text instead of
triggering browser navigation.

## Status Hints

Status hints use the shortest configured gesture for each displayed action,
preserving configuration order when lengths tie. Normal hints show movement,
pane, open, back, filter, command, completed, and quit inputs. Compact hints
show movement, back/open, filter, and command inputs. An active filter shows
the configured filter gesture as its edit hint.

## Acceptance Scenarios

1. A quit-only configuration provides all documented defaults, including
   `h/j/k/l`.
2. Configuring `move_down = ["n"]` enables `n` and disables Down Arrow and `j`
   for that action.
3. Named gestures with Shift, Ctrl, and Alt match only the configured modifiers.
4. Duplicate or conflicting gestures fail startup with an actionable message.
5. Custom command and filter launchers work while command syntax remains
   colon-prefixed.
6. Status hints reflect resolved bindings rather than hardcoded keys.

## References

- [ADR0005: Use Configurable Browser Key Gestures](../adr/ADR0005-use-configurable-browser-key-gestures.md)
- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0003: Slash Todo Filter](SPEC0003-slash-todo-filter.md)
