# PLAN 0061: Fix Overlapping Planner Duration Selection

## Status

Implemented

## Summary

Render every timed item in the Planner's single branch lane and keep duration
branches open when another item starts in the same slot. Generalize timeline
selection so todos and calendar items can each highlight only their own complete
duration.

## Implementation

- Emit one physical timeline row per item in stable display order.
- Render duration starts with `├─`, continuations with `│`, and ends with `└─`;
  only the selected item row replaces its branch with `├▶`.
- Track the selected timeline item's string identity instead of a todo-only
  identity and let `j`/`k` navigate every item stacked in the selected slot.
- Apply the bright accent and content-fitted selected surface to all branches
  belonging to the selected identity without filling the time ruler, unused
  plan width, or an overlapping item's row.
- Keep the single-day and multiday specifications aligned with this grammar.

## Verification

- Cover stacked duration starts, mixed continuation/start slots, complete
  selected task and meeting intervals, and cross-type stack navigation.
- Run `task build`, `task test`, and `graphify update .`.

Validation: all 605 CLI, Core, and TUI tests pass.
