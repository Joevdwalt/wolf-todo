# SPEC 0011: Structured Todo Content Editor

## Status

Accepted

## Purpose

Edit Markdown-backed task fields, multiline content, and nested subtasks without
requiring an external editor or replacing unrelated project content.

## Design

                    WOLF TODO COMPONENTS // TASK EDIT DIALOG
                Sandbox only — Markdown files are never changed.

┌──────────────────────────────────────────────────────────────────────────────┐
│ EDIT TASK // Prepare customer workshop                                       │
│ Title                                                                        │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │Prepare customer workshop                                                 │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ Reference                                                                    │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │ACME-42                                                                   │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ Priority                                                                     │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │High                                                                      │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ Tags                                                                         │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │#client #workshop                                                         │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ Scheduled date (YYYY-MM-DD, t+1, w+1, mon)                                   │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │2026-07-30                                                                │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ Scheduled time                                                               │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │10:30                                                                     │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ Duration                                                                     │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │90m                                                                       │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ Content                                                                      │
│ ╭──────────────────────────────────────────────────────────────────────────╮ │
│ │some content here in multi line                                           │ │
│ │                                                                          │ │
│ │                                                                          │ │
│ ╰──────────────────────────────────────────────────────────────────────────╯ │
│ SUBTASKS                                                                     │
│  ├─ ◯ - Decide on rules                                                      │
│  ├─ ✓ - Send pre-read material                                               │
│  └─ ◯ - Prisesk                                                              │
│                                                                              │
│ j/k MOVE  l EDIT  a ADD  d REMOVE  Space TOGGLE  Ctrl+S SAVE  h CANCEL       │
└──────────────────────────────────────────────────────────────────────────────┘



## Behavior

`e` opens the unified bottom-panel task draft; `E` remains an alias for
configuration compatibility. The draft contains the seven editable task
fields, one `Content` textbox, and a separate `SUBTASKS` list. The Content
textbox contains the task's direct notes as one plain-text multiline value.
Direct subtasks appear in their Markdown source order with one cursor across
the fields, Content, and direct subtasks; a viewport keeps the selection
visible. Open and completed subtasks use `◯` and `✓`. Subtasks with descendants
show a nested-item count.

Movement, open, back, add, edit, completion, removal, and save use configured
bindings. Tab and Shift+Tab move through the fields, Content, and direct
subtasks. While browsing, the fields and Content textbox are read-only. Enter
opens the selected field, Content textbox, or subtask for editing. Content uses a native
multiline text box: Enter creates a new line, Ctrl+S accepts the text into the
task draft, and Escape cancels the text edit. Users enter only the content
text; Markdown list markers and indentation are not required. Ctrl+A selects
the complete value, typing or Enter replaces the selection, Backspace or
Delete removes it, and cursor movement collapses it.

`a` opens a single-line subtask textbox directly; it never opens a content-type
picker. The new subtask is unchecked and is inserted after the selected direct
subtask, or appended when a field or Content is selected. Editing a selected
subtask changes its title and does not permit line breaks. Space toggles a
subtask and reports an error when the selection is a field or Content.

Use the shared form hierarchy: heading styling for section labels, bright
accent for the selection, secondary text for other items, muted hints and
empty states, error styling for validation, and warning styling for destructive
confirmation.

Content may be empty and may contain multiple lines. Existing direct notes are
combined in source order into the Content value. When changed, the value is
stored as one indented Markdown note block: its first line is the list item and
each continuation line is indented below it. New subtasks are unchecked titles.
Opening a subtask in the editor supports arbitrary nesting; its descendants
remain attached to it. Content and subtasks cannot be reordered through this
editor.

Removing a subtask removes its complete subtree. A subtask with nested notes or
children requires confirmation through configured open/back gestures. Changes
remain in memory until Ctrl+S; cancellation writes nothing.

Saving fields, Content, and subtasks re-reads the project, validates the
complete original subtree and the ordered direct-subtask identities, and
applies all changed source lines atomically. Retained subtasks keep their
source order and descendant blocks remain unchanged. New subtasks are inserted
before the next retained direct subtask or at the end of the direct-subtask
block. Stale, duplicated, or reordered subtask identities reject the entire
save. Unchanged Markdown, newline conventions, and permissions are preserved.

## Acceptance Scenarios

1. Task fields, one multiline Content textbox, and direct subtasks appear as
   separate editor sections with one visible selection.
2. Existing direct notes are combined into Content, and Content can be edited
   as multiline plain text without entering Markdown list syntax.
3. `a` opens only subtask entry and inserts an unchecked subtask after the
   selected subtask or appends it from field/Content focus.
4. Subtask titles can be edited, completion can be toggled, and attempting to
   toggle a field or Content reports an error.
5. Nested subtree removal requires confirmation and removes every descendant.
6. Fields, Content, and subtasks can be changed in one draft; Ctrl+S performs
   one atomic write and Escape discards it.
7. External changes or invalid subtask identities reject the save without loss.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
