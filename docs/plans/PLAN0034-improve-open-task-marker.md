# PLAN 0034: Improve the Open Task Marker

## Goal

Make open tasks easier to identify while preserving the established outlined
state symbol and Markdown checkbox storage.

## Implementation

- Use the larger outlined circle `◯` for every open task in the Todos list,
  Planner, inspector, and unified task editor.
- Keep `✓` for completed tasks and centralize state-glyph selection so views
  remain consistent.
- Preserve existing spacing, tree alignment, responsive columns, and theming.
- Update UI specifications, examples, and rendering assertions.

## Verification

- Test open and completed markers across all affected views.
- Run `task test` and `git diff --check`.
