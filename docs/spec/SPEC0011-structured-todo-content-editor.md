# SPEC 0011: Structured Todo Content Editor

## Status

Accepted

## Purpose

Edit Markdown-backed notes and nested subtasks without requiring an external
editor or replacing unrelated project content.

## Behavior

Uppercase `E` opens a bottom-panel draft for the selected todo. Notes and
direct subtasks appear as separate focusable lists. Movement, focus, open,
back, create, edit, completion, removal, and save use configured bindings.

Notes are non-empty single lines. New subtasks are unchecked titles; their
other fields can be changed through the existing todo form after selecting the
subtask. Opening the editor on a subtask supports arbitrary nesting. Reordering
and multiline notes are not supported.

Removing a subtask removes its complete subtree. A subtask with nested notes or
children requires confirmation through configured open/back gestures. Changes
remain in memory until Ctrl+S; cancellation writes nothing.

Saving re-reads the project, validates the complete original subtree, and
applies all changed source lines atomically. Stale content rejects the entire
save. Unchanged Markdown, newline conventions, and permissions are preserved.

## Acceptance Scenarios

1. Notes and direct subtasks can be added, edited, and removed in one draft.
2. Subtask completion can be toggled before saving.
3. Nested subtree removal requires confirmation and removes every descendant.
4. Escape discards the draft and Ctrl+S performs one atomic write.
5. External changes to any selected descendant reject the save without loss.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)

