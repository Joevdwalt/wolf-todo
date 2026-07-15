# PLAN 0012: Implement Global Command Handling

## Status

Implemented

## Goal

Make command mode an application-shell capability so commands work from every
tab without duplicating command state in hosted features.

## Implementation

1. Add shell-owned command state, reducer, and exit/completed operations.
2. Route modal feature input first, then global commands, tab switching, and
   active-feature input.
3. Project command input and errors into both browser and planner status areas.
4. Remove command ownership and exit signaling from the browser reducer.
5. Verify quit, completed visibility, cancellation, errors, and rendering from
   both tabs.
