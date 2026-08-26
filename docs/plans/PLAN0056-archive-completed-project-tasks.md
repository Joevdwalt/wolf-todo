# PLAN 0056: Archive Completed Project Tasks

## Status

Implemented

## Summary

Add `:archive` to move eligible completed root task trees from a selected
project Markdown file into a sibling `*.archive.md` file.

## Changes

- Route the global command through a project-scoped archive mutation.
- Create and append to companion archive files before removing source content.
- Retain completed subtasks beneath an open parent and preserve archived task
  blocks, notes, and completed descendants.
- Report no-op, invalid-context, success, and duplicate-safe partial failures.

## Verification

- Cover command parsing, archive creation/appending, nested-task eligibility,
  source-write failure safety, and application status behavior.
- Run the repository build and test tasks and refresh graphify.
