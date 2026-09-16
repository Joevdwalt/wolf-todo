# PLAN 0066: Reload the TUI on Runtime File Changes

## Goal

Keep the running TUI synchronized with its global configuration and configured
Markdown projects without restarting the process.

## Delivery slices

1. Monitor exact config and project paths with debounced filesystem events.
2. Wake the idle application loop without continuous redraws.
3. Reload valid configuration and catalogs while preserving UI state.
4. Retain conflict-safe editor, bulk-selection, timer, and calendar behavior.
5. Add focused monitor, loop, reload, and application acceptance tests.
