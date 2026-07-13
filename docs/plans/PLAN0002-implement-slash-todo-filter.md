# PLAN 0002: Implement Slash Todo Filter

## Status

Implemented

## Summary

Add `/` as a live, session-only filter for todo rows in the TUI project
browser. Filter the current project or the aggregate `All` view while keeping
the Markdown storage model and project counts unchanged.

## Changes

- Add separate draft and committed filter state to the browser reducer.
- Match todo title, external reference, tags, and section path using a
  case-insensitive substring.
- Apply completed visibility before filtering, preserve matching subtask depth,
  and remove empty project and section headings from filtered results.
- Display filter editing, committed-filter, no-match, and keyboard-hint states
  in the terminal status area.
- Preserve command mode, responsive layouts, and read-only project behavior.

## Verification

- Cover filter entry, live edits, commit, clearing, cancellation, persistence,
  command isolation, field matching, project scope, subtasks, headings, and
  completed visibility with unit tests.
- Run the repository build and test tasks.
