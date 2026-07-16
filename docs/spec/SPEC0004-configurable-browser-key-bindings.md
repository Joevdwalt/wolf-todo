# SPEC 0004: Configurable Browser Key Bindings

## Status

Accepted

## Purpose

Define configurable browser inputs and Vim-style navigation while preserving
the existing keyboard interaction and command syntax.

## Default Interaction

- Down Arrow or `j` moves to the next project or todo.
- Up Arrow or `k` moves to the previous project or todo.
- `g` and `G` jump to the first and last item in the focused browser list.
- Enter or `l` opens or advances from projects to todos to details without
  wrapping at the final pane.
- Esc or `h` returns from details to todos to projects without wrapping at the
  first pane.
- Tab and Shift+Tab cycle focus forward and backward through visible panes.
- Uppercase `L` and `H` select the next and previous application tabs.
- `:` enters command mode, `/` enters filter mode, and `t` opens the sort
  dialog.
- `?` opens the global command palette.
- `v` hides or shows the Todos detail preview.
- `:completed` toggles completed todos and the required configured quit command
  exits the application.
- `[`/`]` change planner dates, `g` selects today, and `u` unschedules.
- `a`, `e`, uppercase `E`, Spacebar, `d`, and Ctrl+S create, edit fields, edit
  content, complete, remove draft content, and save todos.

These inputs are resolved from the global configuration described by ADR0005.
Configured arrays replace the defaults for their action. A custom command-mode
gesture still opens a colon-prefixed command prompt.

While command or filter mode is active, Enter submits, Esc cancels, and
Backspace edits. Printable navigation bindings are entered as text instead of
triggering browser navigation. Application-tab bindings are also ignored while
either input mode is active. The sort dialog also captures input and prevents
application-tab switching until an option is chosen or Esc cancels it.
Planner pickers, move mode, and todo forms capture input in the same way.
The command palette captures input after it opens; its search mode treats
printable navigation bindings as query text.

## Status Hints

Status hints use the shortest configured gesture for each displayed action,
preserving configuration order when lengths tie. Normal hints show movement,
pane, open, back, filter, command, sort, completed, and quit inputs. Compact hints
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
7. Application-tab bindings wrap at the first and last tabs and do not switch
   views while command or filter input is active.
8. A configured sort launcher replaces `t`, participates in conflict
   validation, and is reflected in status hints.
9. Planner and writable-todo hints and actions use their resolved bindings.
10. Content-editor and command-palette launchers use defaults when omitted and
    reject global gesture conflicts when overridden.
11. The configured details toggle updates responsive layouts, focus traversal,
    status hints, and its command-palette action.
12. Browser `g` and Planner `g` may coexist because they are handled in
    disjoint views; conflicting browser actions still fail startup.

## References

- [ADR0005: Use Configurable Browser Key Gestures](../adr/ADR0005-use-configurable-browser-key-gestures.md)
- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0003: Slash Todo Filter](SPEC0003-slash-todo-filter.md)
- [SPEC0005: Application View Tabs](SPEC0005-application-view-tabs.md)
- [SPEC0006: Todo Sorting](SPEC0006-todo-sorting.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0011: Structured Todo Content Editor](SPEC0011-structured-todo-content-editor.md)
- [SPEC0012: Global Command Palette](SPEC0012-global-command-palette.md)
