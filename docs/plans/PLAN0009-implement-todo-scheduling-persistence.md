# PLAN 0009: Implement Todo Scheduling Persistence

## Status

Implemented

## Implementation

1. Parse a typed half-hour schedule from paired Markdown metadata.
2. Add schedule, create, edit, and completion mutations with stale-target
   validation and atomic file replacement.
3. Preserve surrounding Markdown, newlines, and source permissions.
4. Reload the catalog and restore source identity after successful writes.
