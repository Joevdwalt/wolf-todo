# Show Current Time in the Interface

## Summary

Show the live local time throughout the main TUI views while preserving the
existing Planner current-time marker and timer refresh behavior.

## Implementation

- Centralize the duplicated Todos and Planner operational-header renderer.
- Add a semantic `TIME:HH:mm` segment before lower-priority responsive header
  fields, and include the same clock in focus mode's compact header.
- Reuse injected clock providers and pass one timestamp through each Planner
  frame so its header and `NOW` row agree.
- Redraw Todos and focus mode after an idle minute; keep active timer and
  Planner calendar-refresh intervals unchanged.

## Verification

- Cover deterministic clock rendering, narrow-width visibility, focus mode,
  Planner/header timestamp consistency, and idle redraw behavior.
- Run `task build`, `task test`, `git diff --check`, and `graphify update .`.
