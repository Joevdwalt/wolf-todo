# PLAN 0095: Project Checklist Tree

## Status

Implemented.

## Plan

1. Extend the Todos task list with rounded final branches and `●` for the
   selected open task, retaining existing columns, cursor, styles, and marks.
2. Add an optional final-branch glyph to the shared tree formatter. Reuse it in
   focus mode and preserve the default for inspectors. Split the formatter and
   tree-segment enum into separate files per ADR0012.
3. Preserve tag-line guides, collapse/filter behavior, navigation, editing,
   task links, completion, and Markdown storage. Use the shared selected-state
   glyph for floating-palette placement so the selected task stays visible.
4. Update the browser/design-system specifications and README. Verify rendering
   and constrained layouts, run `task test`, and refresh `graphify update .`.

## Verification

- Renderer, tree formatter, focus, and terminal tests: 155 passed, including
  narrow layouts, tag guides, scrolling, and floating-palette placement.
- `task test`: all 994 tests passed, including browser selection, filtering,
  saved views, editing, completion, and task-link workflows.
- `graphify update .`: refreshed the code graph.
