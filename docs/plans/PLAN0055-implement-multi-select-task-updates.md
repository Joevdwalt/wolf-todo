# PLAN 0055: Implement Multi-Select Task Updates

## Status

Implemented

## Summary

Add transient cross-project task marks and one bulk editor for schedule, tags,
priority, and completion while preserving Markdown-file safety.

## Changes

- Add configurable mark, bulk-edit, and clear-selection actions to the Todos
  reducer, status hints, and command palette.
- Extract browser task rows into a focused component that owns responsive
  title, tag, cursor, completion, and marked presentation.
- Add a bulk editor with explicit unchanged/set/clear or tag-operation modes.
- Validate and update every selected task in a project through one atomic write,
  then orchestrate cross-project groups with partial-failure reporting.
- Keep single-task actions unchanged and clear transient marks when their
  source-line identities can no longer be trusted.

## Verification

- Cover core batch mutation, selection state, form behavior, configuration,
  rendering, cross-project orchestration, and failure handling with focused
  tests.
- Run the repository build and test tasks and refresh graphify.
