# PLAN 0033: Show a Live Planner Time Line

## Goal

Show the current time in today's Day Planner without making the marker look
like another panel or table border.

## Implementation

- Add a logical timeline row containing the exact time and an accent-bright,
  full-width `▶────` line immediately before the next half-hour slot.
- Size the line from the actual allocated Plan cell width so it stays on one row.
- Pin the logical row before 06:00 or after 21:30 at the matching planner
  boundary, while preserving selected-slot viewport priority.
- Count the marker in responsive row budgets and leave its background and the
  existing blue table borders unchanged.
- Add a timed terminal input read and redraw the active Planner after one idle
  minute without changing state.
- Inject the current clock into terminal rendering for deterministic tests.

## Verification

- Test marker placement, colors, full-width line styling, boundary behavior,
  non-today omission, selection priority, responsive heights, and idle redraw.
- Run `task test` and `git diff --check`.
