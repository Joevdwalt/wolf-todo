# PLAN 0086: Show Subtasks in Search Results

## Status

Implemented

## Summary

Show each search-matching task's eligible recursive subtask tree in the Todos
list, even when the descendants do not match the search text.

## Changes

- Carry a qualifying search match through the visible-tree construction.
- Preserve completion visibility, `@today` and saved-view criteria, ancestor
  context, sorting, and tree connectors.
- Document the behavior in SPEC0003 and the README.

## Verification

- Cover matching parents and descendants, unrelated branches, completion
  visibility, virtual views, and visible tree identities and connectors.
- Run `task build` and `task test`.
