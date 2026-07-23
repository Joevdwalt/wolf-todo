# PLAN 0042: Make Planner All Day Interactive

## Goal

Allow date-only todos to be created, assigned, moved, edited, completed, and
unscheduled directly from the Day Planner.

## Implementation

- Add planner focus and selection state for timeline and all-day panes.
- Reuse configured pane and navigation bindings across both panes.
- Represent schedule destinations explicitly as timed or all-day.
- Require only a date for all-day creation and preserve duration while moving.
- Make all-day calendar entries selectable for details but read-only.
- Keep the all-day pane accessible independently of Inspector visibility and
  expose an empty insertion target on compact layouts.

## Verification

- Cover focus, navigation, create, assign, move, edit, completion, unscheduling,
  duration preservation, calendar read-only behavior, and responsive rendering.
- Run `task test` and `git diff --check`.
