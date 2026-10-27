# PLAN 0090: Floating Command Palette

## Status

Implemented

## Implementation

- Float the existing select-list palette over the Todos, Day Planner, and focus
  workspaces at 80 columns and 24 rows or larger, using the control layering API.
- Place it opposite the selected row, cap its height to keep that row visible,
  and leave header and status outside the overlay.
- Keep the full-width bottom panel on narrower or shorter terminals. Preserve
  command state, input routing, bindings, and Markdown storage.

## Verification

- Cover floating and fallback sizes, selection visibility, and all three views
  with terminal rendering tests.
- Run `task test` and `task build`.
