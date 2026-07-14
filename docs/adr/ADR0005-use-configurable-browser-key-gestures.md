# ADR 0005: Use Configurable Browser Key Gestures

## Status

Accepted

## Context

The project browser originally hardcoded navigation, command-mode, and filter
inputs while exposing only the quit command through global configuration. Wolf
Todo needs Vim-style navigation and user-defined bindings without invalidating
existing configuration files.

## Decision

Extend the global `[keybindings]` table with optional browser-action arrays and
the completed command:

```toml
[keybindings]
quit = ":q"
toggle_completed = ":completed"
move_up = ["UpArrow", "k"]
move_down = ["DownArrow", "j"]
focus_next = ["Tab"]
focus_previous = ["Shift+Tab"]
open = ["Enter", "l"]
back = ["Escape", "h"]
command_mode = [":"]
filter_mode = ["/"]
sort_mode = ["t"]
tab_next = ["Ctrl+Tab"]
tab_previous = ["Ctrl+Shift+Tab"]
```

`quit` remains required. Every omitted field uses the value shown above; an
explicit array replaces rather than extends its default. This keeps existing
quit-only configuration valid while allowing built-in bindings to be removed.

A gesture is either one printable character or a case-insensitive `ConsoleKey`
name with optional `Shift`, `Ctrl`/`Control`, or `Alt` modifiers joined by `+`.
Character matching is case-sensitive and named-key modifier matching is exact.

Reject empty or malformed bindings, duplicates, gestures assigned to more than
one TUI action, and identical quit and completed commands. Command-launcher
bindings do not change command syntax: command input continues to begin with
`:`. Enter, Esc, and Backspace remain fixed editing controls inside command and
filter modes. The sort launcher is configurable, while the option keys shown
inside its modal dialog are fixed editing controls.

## Consequences

- Vim `h/j/k/l` navigation works by default without removing arrow, Tab, Enter,
  or Esc behavior.
- Existing users do not need to migrate their configuration.
- Startup validation prevents reducer-order-dependent binding conflicts.
- Hosts and terminal rendering must consume the same resolved binding object so
  behavior and status hints remain consistent.

## References

- [ADR0004: Use a Global TOML Configuration](ADR0004-use-a-global-toml-configuration.md)
- [SPEC0004: Configurable Browser Key Bindings](../spec/SPEC0004-configurable-browser-key-bindings.md)
- [SPEC0005: Application View Tabs](../spec/SPEC0005-application-view-tabs.md)
- [SPEC0006: Todo Sorting](../spec/SPEC0006-todo-sorting.md)
