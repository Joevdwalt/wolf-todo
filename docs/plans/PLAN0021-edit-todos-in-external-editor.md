# PLAN 0021: Edit Todos in the External Editor

## Status

Implemented

## Implementation

- Add a configurable Ctrl+E Todos action and command-palette entry that opens
  the selected Markdown project through `$EDITOR`.
- Position known terminal editors at the todo source line without invoking a
  shell, and surface missing editors, launch failures, and exit failures.
- Suspend and reset terminal rendering around the editor process, reload the
  catalog after a launch, and retain browser context without stale identities.
