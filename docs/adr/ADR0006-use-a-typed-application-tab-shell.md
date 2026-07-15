# ADR 0006: Use a Typed Application Tab Shell

## Status

Accepted

## Context

The TUI originally ran the project browser directly. Wolf Todo needs room for
additional fixed views, such as a day planner, while preserving independent
navigation and editing state for every view. A runtime plugin registry would
add discovery and lifecycle complexity before the set of application views is
dynamic.

## Decision

Place a typed application shell above TUI features. The shell owns an ordered,
non-empty set of tab definitions, the active tab identifier, and the state of
each hosted feature. Tabs are fixed application views declared in code and are
not closable or user-reorderable.

Implement tab state, wraparound movement, and presentation as a reusable
component that does not depend on browser state. The application shell routes
input to the active feature and preserves every feature's state while another
tab is selected. A feature that is capturing command or filter input takes
precedence over application-tab shortcuts.

Always render a one-line tab strip, including when only one view exists. The
initial application registers only the existing `Todos` browser. Adding a day
planner will be a separate feature and storage decision; this ADR does not
define planner behavior or change Markdown persistence.

Use TUI-wide configurable bindings for next and previous tab selection. Their
defaults are uppercase `L` and `H`, avoiding combinations commonly consumed by
terminal emulators before a TUI receives them. Rename the resolved binding
model from a browser-specific name so the shell and browser consume the same
validated configuration.

## Consequences

### Positive

- New fixed TUI views can be composed without coupling them to the browser.
- Switching views preserves feature-local selection, focus, and draft state.
- Tab navigation and rendering can be tested independently of terminal I/O.
- The always-visible strip gives the layout a stable place for future views.

### Negative

- The initial one-tab application gains a shell layer before a second view is
  implemented.
- Every hosted feature must have an explicit shell state and input route.
- Terminal height calculations must reserve a row for the tab strip.

## References

- [ADR0003: Structure Source Code for Testability](ADR0003-structure-source-code-for-testability.md)
- [ADR0005: Use Configurable Browser Key Gestures](ADR0005-use-configurable-browser-key-gestures.md)
- [SPEC0005: Application View Tabs](../spec/SPEC0005-application-view-tabs.md)
