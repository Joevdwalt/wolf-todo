# SPEC 0005: Application View Tabs

## Status

Accepted

## Purpose

Define the reusable TUI tab component and application shell that allow Wolf
Todo to host multiple fixed views while keeping Markdown-file storage central.
The first release contains only the existing `Todos` browser; a day planner is
a future consumer rather than part of this specification.

## Tab Model and State

- Define tabs in a fixed, ordered, non-empty collection with stable identifiers
  and display titles.
- Keep one active tab identifier and reject state whose active identifier is
  absent from the collection.
- Tabs are not closable, reorderable, or dynamically discovered.
- Keep each hosted feature's state in the typed application state. Selecting a
  different tab must not recreate or reset inactive feature state.

## Presentation

Render the tab strip on one line above the active feature, even when only one
tab exists. Style the active title in cyan and bold and inactive titles dimly.
Escape tab titles before rendering and truncate the strip with an ellipsis when
it exceeds the terminal width.

When more than one tab exists, append the shortest configured next-tab gesture
and the label `tabs`. Do not show a switching hint for a single tab.

## Interaction

- Ctrl+Tab selects the next tab by default.
- Ctrl+Shift+Tab selects the previous tab by default.
- Movement wraps from the final tab to the first and from the first to the
  final tab.
- Movement is a no-op when only one tab exists.
- Configured bindings replace the defaults through the global `[keybindings]`
  table and participate in the same duplicate/conflict validation as other TUI
  bindings.
- While the active feature captures command or filter input, all keystrokes are
  routed to that feature and tab selection is disabled.
- Otherwise, application-tab input is handled by the shell and remaining input
  is routed to the active feature.

## Future Views

A future `Day Planner` tab may maintain its own selected date, timeslot, and
draft state. Its scheduling semantics and Markdown representation require a
separate specification and architecture decision. The tab component must not
introduce a second persistence model.

## Acceptance Scenarios

1. The initial application always shows one selected `Todos` tab above the
   existing browser.
2. A one-tab host ignores next- and previous-tab input and omits the switch
   hint.
3. A host with multiple tabs wraps selection in both directions.
4. Switching away from a view and back preserves that view's state.
5. Command and filter input receive tab gestures as input and do not switch the
   active view.
6. Custom tab bindings replace defaults and conflicting gestures fail startup.
7. A narrow terminal truncates the tab strip without wrapping or throwing.

## References

- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
- [ADR0005: Use Configurable Browser Key Gestures](../adr/ADR0005-use-configurable-browser-key-gestures.md)
- [ADR0006: Use a Typed Application Tab Shell](../adr/ADR0006-use-a-typed-application-tab-shell.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
