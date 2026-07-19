# PLAN 0030: Unify the Todo Content Outline

## Goal

Replace the separate Notes and Subtasks sections with one source-ordered
bottom-panel outline while retaining conflict-safe Markdown writes.

## Implementation

- Merge direct notes and subtasks into a typed ordered draft with one cursor.
- Render notes and completion-aware subtasks together, including nested counts
  and a selected-item viewport.
- Use a configured-binding Note/Subtask picker and insert new content after the
  current selection.
- Carry ordered content through the core mutation API, preserving retained
  source order and complete descendant blocks.
- Keep the property form, item reordering, type conversion, and multiline notes
  outside this change.

## Verification

- Test source ordering, navigation, type selection, insertion, editing,
  completion, deletion confirmation, responsive rendering, and atomic saves.
- Run `task test` and `git diff --check`.
