# PLAN 0019: Implement Priority Sorting

## Status

Implemented

## Implementation

- Add priority as a persisted sort property without changing existing enum
  values in session state.
- Map `p` to Lowest-through-Highest ordering and `P` to the reverse, keeping
  unprioritized todos last in both directions.
- Extend the responsive sort dialog and active-sort hint while preserving todo
  grouping, stable ties, nested blocks, and selection restoration.
