# ADR 0011: Use Configurable TUI Surfaces

## Status

Accepted

## Context

ADR0008 introduced semantic foreground roles and deliberately preserved the
terminal background. Wolf Todo now needs a cohesive orange-and-blue palette
with a dark canvas, layered workspaces, elevated overlays, active borders, and
selected-row fills. Applying backgrounds directly to individual text fragments
would leave padding and empty cells unpainted.

## Decision

Extend `TuiTheme` with `background`, `surface`, `surface_2`,
`secondary_text`, `border_active`, `accent_bright`, and `info`. Keep every
ADR0008 role and configuration key compatible. The Wolf preset supplies the
complete palette; Classic and Mono use terminal-default surfaces.

Apply backgrounds through a renderable decorator that preserves existing
foregrounds and explicit nested backgrounds, and paints padding to the
renderable's allocated width without adding lines. Use the background for the
application header/canvas, the primary surface for workspaces, and the second
surface for inspectors, status panels, dialogs, and selected rows.

## Consequences

- The default Wolf interface has predictable contrast independent of the
  terminal palette.
- Users can set a surface to `default` so it inherits its enclosing or terminal
  background.
- Nested renderables can establish an elevated surface without being
  overwritten by their parent surface.
- Rendering tests must verify both cell coverage and terminal height because
  accidental padding lines can scroll the application header.

## References

- [ADR0008: Use Semantic TUI Themes](ADR0008-use-semantic-tui-themes.md)
- [SPEC0007: Configurable TUI Themes](../spec/SPEC0007-configurable-tui-themes.md)
- [SPEC0013: Operational Console Design System](../spec/SPEC0013-operational-console-design-system.md)
