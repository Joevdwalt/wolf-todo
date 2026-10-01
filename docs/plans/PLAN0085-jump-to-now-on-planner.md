# PLAN 0085: Jump to Now on the Planner

## Goal

Make the existing `planner_today` action jump to today's current planner slot,
so `T` returns the user to the live position instead of only changing the date.

## Implementation

- Inject the planner clock and map local time to the containing 15-minute slot.
- Clamp before 06:00 to the first slot and at or after 21:45 to the last slot.
- Focus the timeline, clear stale overlap selection, and re-anchor multiday
  ranges around today without changing Markdown schedules.
- Keep the `planner_today` key and configuration name; update palette, footer,
  README, and planner specifications to call the action `NOW`.

## Verification

- Cover ordinary, exact-boundary, out-of-hours, multiday, keyboard, and palette
  behavior with deterministic clock providers.
- Run `task test`, `git diff --check`, and `graphify update .`.
