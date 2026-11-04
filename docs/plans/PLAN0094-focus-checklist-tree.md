# PLAN 0094: Focus Checklist Tree

## Status

Implemented.

## Plan

1. Keep the root title in place and adapt only the focus checklist to use
   `├─`, `╰─`, and ancestor continuation bars, including the selected task's link row.
2. Use `●` for the selected open checklist item, `◯` for other open items,
   and `✓` for completed items; retain selection colors and emphasis.
3. Update SPEC0022 and add focused renderer coverage for hierarchy, selection,
   link visibility, and constrained terminal sizes. Limit link indentation to
   the available width. Preserve task interactions and Markdown storage.
4. Run the focus tests and `task test`, then run `graphify update .`.

## Verification

- Focus tests: 16 passed, including flat and nested selections at widths
  32–80 and heights 15–16.
- `task test`: all 977 tests passed.
