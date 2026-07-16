# PLAN 0017: Persist Browser Session State

## Status

Implemented

## Implementation

- Extend the best-effort application state snapshot with the selected todo sort
  while remaining compatible with existing path-only JSON.
- Restore the configured project and valid sort independently, defaulting stale
  projects to `All` and absent or invalid sorts to source order.
- Always launch on the Todos tab with focus in the Todos pane, and save the
  retained browser project and sort even when exiting from Day Planner.
- Keep todo selection, filters, visibility switches, modal state, active tab,
  focus, and planner state transient.
