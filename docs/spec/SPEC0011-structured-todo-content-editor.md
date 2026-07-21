# SPEC 0011: Structured Todo Content Editor

## Status

Accepted

## Purpose

Edit Markdown-backed notes and nested subtasks without requiring an external
editor or replacing unrelated project content.

## Behavior

`e` opens the unified bottom-panel task draft; `E` remains an alias for
configuration compatibility. Direct notes and subtasks appear below the six
editable task fields in one outline ordered by their Markdown source lines,
with one cursor and a viewport that keeps the selection visible. Notes use `•`;
open and completed subtasks use `◯` and `✓`. Subtasks with descendants show
a nested-item count.

Movement, open, back, create, edit, completion, removal, and save use configured
bindings. Creating opens a two-item Note/Subtask picker controlled by configured
move, open, and back actions; it defaults to Note. The accepted item is inserted
after selected content; when a field is selected, it appends to the outline.
Open and edit change the selected field or item's text. Notes open in a native
multiline text box: Enter creates a new line, Ctrl+S accepts that text into the
task draft, and Escape cancels the text edit. Subtasks use the same text box in
single-line mode. Space toggles a subtask and reports an error when the
selection is a field or note.

Use the shared form hierarchy: heading styling for the outline and picker
labels, bright accent for the selection, secondary text for other items, muted
hints and empty states, error styling for validation, and warning styling for
destructive confirmation.

Notes are non-empty and may contain multiple lines. Their first line is stored
as an indented Markdown list item, while each continuation line is indented
below that item. New subtasks are unchecked titles; their other fields can be
changed by opening that subtask in the same editor. Opening the editor on a
subtask supports arbitrary nesting. Existing items cannot change type.
Reordering is not supported.

Removing a subtask removes its complete subtree. A subtask with nested notes or
children requires confirmation through configured open/back gestures. Changes
remain in memory until Ctrl+S; cancellation writes nothing.

Saving fields and content re-reads the project, validates the complete original subtree and the
ordered direct-item identities, and applies all changed source lines atomically.
Retained items keep their source order and subtask descendant blocks remain
unchanged. New items are inserted before the next retained item or at the end
of the direct-content block. Stale, duplicated, retyped, or reordered source
items reject the entire save. Unchanged Markdown, newline conventions, and
permissions are preserved.

## Acceptance Scenarios

1. Interleaved notes and direct subtasks appear in Markdown source order with
   one selection.
2. The type picker adds a note or subtask immediately after the selected item.
3. Items can be edited and removed; subtask completion can be toggled before
   saving, while attempting to toggle a note reports an error.
4. Nested subtree removal requires confirmation and removes every descendant.
5. Fields and ordered content can be changed in one draft; Ctrl+S performs one
   atomic write and Escape discards it.
6. External changes or invalid ordered identities reject the save without loss.
7. A note can be edited across multiple lines, saved into its task draft with
   Ctrl+S, and cancelled with Escape without changing that draft.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
