# PLAN 0081: Constrain Inspector Height

## Summary

Prevent multiline or wrapped details from pushing the TUI beyond the terminal
viewport and cropping its header.

## Changes

- Constrain Planner and Todos inspector content by rendered terminal rows.
- Preserve wrapping and styles, then clip overflow with an ellipsis.
- Cover multiline notes in responsive rendering tests.

## Verification

- Run the full test task.
- Refresh the Graphify index.
