# PLAN 0083: Simplify Multiday Planner Cell

## Changes

- Move overlap ordering, selected visibility, widths, and row compaction into a pure cell layout class.
- Keep the cell renderer responsible for styles and Spectre columns.
- Reserve narrow-cell space for a finish cue before truncating the title; omit the cue only when it cannot fit.
- Size continuation lanes from their start titles so overlap separators remain aligned.
- Keep later items in their lanes after earlier overlapping items finish.

## Verification

- Test ordering, selected overflow, natural widths, Unicode truncation, and cue priority directly in the layout.
- Test an exact-fit cue in the rendered cell and run the TUI suite.
- Refresh Graphify after code changes.
