# PLAN 0016: Implement Vim List Jumps

## Status

Implemented

## Implementation

- Add configurable `g` and `G` browser actions for the first and last item in
  the focused Projects or Todos list.
- Allow `g` to remain Planner's contextual today action while rejecting
  conflicts within the Todos browser.
- Expose both semantic actions through the command palette and cover direct,
  configured, and palette bindings with tests.
